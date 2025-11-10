using API_NoSQL.Dtos;
using API_NoSQL.Models;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class ReviewService
    {
        private readonly MongoDbContext _ctx;
        private readonly CustomerService _customers;
        private readonly BookService _books;

        public ReviewService(MongoDbContext ctx, CustomerService customers, BookService books)
        {
            _ctx = ctx;
            _customers = customers;
            _books = books;
        }

        public async Task<(bool Ok, string? Error, BookReview? Review)> CreateReviewAsync(
            string customerCode, CreateReviewDto dto)
        {
            try
            {
                var customer = await _customers.GetByCodeAsync(customerCode);
                if (customer == null)
                    return (false, "Customer not found", null);

                var order = customer.Orders.FirstOrDefault(o => o.Code == dto.OrderCode);
                if (order == null)
                    return (false, "Order not found", null);

                if (order.Status != "HoanThanh")
                    return (false, "Only completed orders can be reviewed", null);

                var orderItem = order.Items.FirstOrDefault(i => i.BookCode == dto.BookCode);
                if (orderItem == null)
                    return (false, "Book not found in this order", null);

                var book = await _books.GetByCodeAsync(dto.BookCode);
                if (book == null)
                    return (false, "Book not found", null);

                var existingReview = book.Reviews?.FirstOrDefault(r => 
                    r.CustomerCode == customerCode && r.OrderCode == dto.OrderCode);
                if (existingReview != null)
                    return (false, "You have already reviewed this book for this order", null);

                if (dto.Rating < 1 || dto.Rating > 5)
                    return (false, "Rating must be between 1 and 5", null);

                var review = new BookReview
                {
                    ReviewId = Guid.NewGuid().ToString("N"),
                    CustomerCode = customerCode,
                    ReviewerName = customer.FullName,
                    AvatarUrl = customer.Avatar,
                    OrderCode = dto.OrderCode,
                    Rating = dto.Rating,
                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                var update = Builders<Book>.Update.Push(b => b.Reviews, review);
                await _ctx.Books.UpdateOneAsync(b => b.Code == dto.BookCode, update);

                await UpdateAverageRatingAsync(dto.BookCode);

                return (true, null, review);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Ok, string? Error)> UpdateReviewAsync(
            string customerCode, string bookCode, string reviewId, UpdateReviewDto dto)
        {
            try
            {
                var book = await _books.GetByCodeAsync(bookCode);
                if (book?.Reviews == null)
                    return (false, "Book or reviews not found");

                var review = book.Reviews.FirstOrDefault(r => r.ReviewId == reviewId);
                if (review == null)
                    return (false, "Review not found");

                if (review.CustomerCode != customerCode)
                    return (false, "You can only edit your own reviews");

                if (dto.Rating.HasValue)
                {
                    if (dto.Rating < 1 || dto.Rating > 5)
                        return (false, "Rating must be between 1 and 5");
                    review.Rating = dto.Rating.Value;
                }

                if (dto.Content != null)
                    review.Content = dto.Content;

                review.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<Book>.Filter.And(
                    Builders<Book>.Filter.Eq(b => b.Code, bookCode),
                    Builders<Book>.Filter.ElemMatch(b => b.Reviews, r => r.ReviewId == reviewId)
                );

                var update = Builders<Book>.Update.Set("danhgia.$", review);
                var result = await _ctx.Books.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                    return (false, "Failed to update review");

                await UpdateAverageRatingAsync(bookCode);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Ok, string? Error)> DeleteReviewAsync(
            string customerCode, string bookCode, string reviewId)
        {
            try
            {
                var book = await _books.GetByCodeAsync(bookCode);
                if (book?.Reviews == null)
                    return (false, "Book or reviews not found");

                var review = book.Reviews.FirstOrDefault(r => r.ReviewId == reviewId);
                if (review == null)
                    return (false, "Review not found");

                if (review.CustomerCode != customerCode)
                    return (false, "You can only delete your own reviews");

                var update = Builders<Book>.Update
                    .PullFilter(b => b.Reviews, r => r.ReviewId == reviewId);

                var result = await _ctx.Books.UpdateOneAsync(b => b.Code == bookCode, update);

                if (result.ModifiedCount == 0)
                    return (false, "Failed to delete review");

                await UpdateAverageRatingAsync(bookCode);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        public async Task<List<ReviewResponseDto>> GetReviewsByBookAsync(string bookCode, string? currentCustomerCode = null)
        {
            var book = await _books.GetByCodeAsync(bookCode);
            if (book?.Reviews == null)
                return new List<ReviewResponseDto>();

            var customerCodes = book.Reviews.Select(r => r.CustomerCode).Distinct().ToList();

            var customers = await _ctx.Customers
                .Find(c => customerCodes.Contains(c.Code))
                .Project(c => new { c.Code, c.FullName, c.Avatar })
                .ToListAsync();

            var customerMap = customers.ToDictionary(c => c.Code);

            return book.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(r =>
                {
                    // ✅ Lấy tên và avatar mới nhất từ customer
                    var customer = customerMap.GetValueOrDefault(r.CustomerCode);

                    return new ReviewResponseDto
                    {
                        ReviewId = r.ReviewId,
                        CustomerCode = r.CustomerCode,
                        ReviewerName = customer?.FullName ?? r.ReviewerName,
                        AvatarUrl = customer?.Avatar ?? r.AvatarUrl,
                        OrderCode = r.OrderCode,
                        Rating = r.Rating,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        CanEdit = r.CustomerCode == currentCustomerCode
                    };
                })
                .ToList();
        }

        private async Task UpdateAverageRatingAsync(string bookCode)
        {
            var book = await _books.GetByCodeAsync(bookCode);
            if (book == null) return;

            var avgRating = book.Reviews?.Any() == true
                ? book.Reviews.Average(r => r.Rating)
                : 0;

            var update = Builders<Book>.Update.Set(b => b.AverageRating, avgRating);
            await _ctx.Books.UpdateOneAsync(b => b.Code == bookCode, update);
        }

        public async Task<bool> CanReviewBookAsync(string customerCode, string bookCode)
        {
            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer == null) return false;

            return customer.Orders.Any(o =>
                o.Status == "HoanThanh" &&
                o.Items.Any(i => i.BookCode == bookCode));
        }
    }
}