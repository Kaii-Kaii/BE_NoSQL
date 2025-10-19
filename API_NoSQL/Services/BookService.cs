using API_NoSQL.Models;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class BookService
    {
        private readonly MongoDbContext _ctx;

        public BookService(MongoDbContext ctx) => _ctx = ctx;

        public async Task<(IReadOnlyList<Book> Items, long Total)> SearchAsync(
            string? keyword, string? categoryCode, int? minPrice, int? maxPrice,
            string? sortBy, bool desc, int page = 1, int pageSize = 20)
        {
            var fb = Builders<Book>.Filter;
            var filter = fb.Empty;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");
                filter &= fb.Or(
                    fb.Regex(b => b.Name, regex),
                    fb.Regex(b => b.Author, regex),
                    fb.Regex(b => b.Code, regex));
            }

            if (!string.IsNullOrWhiteSpace(categoryCode))
                filter &= fb.Eq("loai.maloai", categoryCode);

            if (minPrice.HasValue) filter &= fb.Gte(b => b.Price, minPrice);
            if (maxPrice.HasValue) filter &= fb.Lte(b => b.Price, maxPrice);

            var sortDef = sortBy?.ToLower() switch
            {
                "price" => desc ? Builders<Book>.Sort.Descending(b => b.Price) : Builders<Book>.Sort.Ascending(b => b.Price),
                "sold" => desc ? Builders<Book>.Sort.Descending(b => b.Sold) : Builders<Book>.Sort.Ascending(b => b.Sold),
                "name" => desc ? Builders<Book>.Sort.Descending(b => b.Name) : Builders<Book>.Sort.Ascending(b => b.Name),
                _ => Builders<Book>.Sort.Ascending(b => b.Name)
            };

            var total = await _ctx.Books.CountDocumentsAsync(filter);
            var items = await _ctx.Books.Find(filter)
                .Sort(sortDef)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<Book?> GetByCodeAsync(string code) =>
            _ctx.Books.Find(b => b.Code == code).FirstOrDefaultAsync();

        public async Task CreateAsync(Book book) =>
            await _ctx.Books.InsertOneAsync(book);

        public async Task<bool> UpdateByCodeAsync(string code, Action<Book> update)
        {
            var book = await GetByCodeAsync(code);
            if (book is null) return false;
            update(book);
            var res = await _ctx.Books.ReplaceOneAsync(b => b.Id == book.Id, book);
            return res.ModifiedCount == 1;
        }

        public async Task<bool> DeleteByCodeAsync(string code)
        {
            var res = await _ctx.Books.DeleteOneAsync(b => b.Code == code);
            return res.DeletedCount == 1;
        }

        public async Task<bool> AdjustStockAndSoldAsync(string code, int qty)
        {
            // Decrease stock, increase sold when purchasing
            var update = Builders<Book>.Update
                .Inc(b => b.InStock, -qty)
                .Inc(b => b.Sold, qty);

            var res = await _ctx.Books.UpdateOneAsync(
                b => b.Code == code && b.InStock >= qty,
                update);

            return res.ModifiedCount == 1;
        }

        // NEW: get all books (no paging/filter)
        public Task<List<Book>> GetAllAsync() =>
            _ctx.Books.Find(Builders<Book>.Filter.Empty).ToListAsync();

        // NEW: top sold books
        public Task<List<Book>> GetTopSoldAsync(int limit) =>
            _ctx.Books.Find(Builders<Book>.Filter.Empty)
                .Sort(Builders<Book>.Sort.Descending(b => b.Sold))
                .Limit(limit)
                .ToListAsync();
    }
}