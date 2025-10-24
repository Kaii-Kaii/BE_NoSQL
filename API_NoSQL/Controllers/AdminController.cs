using API_NoSQL.Dtos;
using API_NoSQL.Security;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RoleAuthorize("admin")] // use header: X-Role: admin
    public class AdminController : ControllerBase
    {
        private readonly OrderService _orders;
        private readonly BookService _books;
        private readonly CloudinaryService _cloudinary;

        public AdminController(OrderService orders, BookService books, CloudinaryService cloudinary)
        {
            _orders = orders;
            _books = books;
            _cloudinary = cloudinary;
        }

        // LIST BOOKS FOR ADMIN (reuse search)
        // GET /api/Admin/books
        [HttpGet("books")]
        public async Task<IActionResult> GetBooks(
            [FromQuery] string? keyword,
            [FromQuery] string? categoryCode,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _books.SearchAsync(
                keyword, categoryCode, minPrice, maxPrice, sortBy, desc, page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }

        // GET /api/Admin/orders?page=1&pageSize=20
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var (items, total) = await _orders.GetAllOrdersAsync(page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }

        // PUT /api/Admin/orders/{customerCode}/{orderCode}/status
        [HttpPut("orders/{customerCode}/{orderCode}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string customerCode, string orderCode, [FromBody] UpdateOrderStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { error = "Status is required" });

            var ok = await _orders.UpdateOrderStatusAsync(customerCode, orderCode, dto.Status);
            if (!ok)
                return BadRequest(new { error = "Failed to update order status. Check customer code, order code, and valid status." });

            return NoContent();
        }

        // BOOK MANAGEMENT (admin)
        // POST /api/Admin/books (multipart/form-data with optional image file `cover`)
        [HttpPost("books")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBook([FromForm] AdminBookCreateFormDto dto)
        {
            var existing = await _books.GetByCodeAsync(dto.Code);
            if (existing is not null) return Conflict(new { error = $"Book code {dto.Code} already exists." });

            string? coverUrl = null;
            if (dto.Cover is not null && dto.Cover.Length > 0)
            {
                coverUrl = await _cloudinary.UploadImageAsync(dto.Cover);
            }

            var b = new Models.Book
            {
                Code = dto.Code,
                Name = dto.Name,
                Author = dto.Author,
                PublishYear = dto.PublishYear,
                Price = dto.Price,
                InStock = dto.InStock,
                Description = dto.Description,
                CoverUrl = coverUrl,
                Status = dto.Status,
                Category = dto.Category,
                Publisher = dto.Publisher,
                Sold = 0
            };

            await _books.CreateAsync(b);
            return Created($"/api/Books/{b.Code}", new { b.Code, b.CoverUrl });
        }

        // PUT /api/Admin/books/{code} (multipart/form-data with optional image `cover`)
        [HttpPut("books/{code}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBook(string code, [FromForm] AdminBookUpdateFormDto dto)
        {
            string? coverUrl = null;
            if (dto.Cover is not null && dto.Cover.Length > 0)
            {
                coverUrl = await _cloudinary.UploadImageAsync(dto.Cover);
            }

            var ok = await _books.UpdateByCodeAsync(code, b =>
            {
                if (dto.Name is not null) b.Name = dto.Name;
                if (dto.Author is not null) b.Author = dto.Author;
                if (dto.PublishYear.HasValue) b.PublishYear = dto.PublishYear.Value;
                if (dto.Price.HasValue) b.Price = dto.Price.Value;
                if (dto.InStock.HasValue) b.InStock = dto.InStock.Value;
                if (dto.Description is not null) b.Description = dto.Description;
                if (coverUrl is not null) b.CoverUrl = coverUrl; // set new uploaded url if any
                if (dto.Status is not null) b.Status = dto.Status;
                if (dto.Category is not null) b.Category = dto.Category;
                if (dto.Publisher is not null) b.Publisher = dto.Publisher;
            });
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/Admin/books/{code}
        [HttpDelete("books/{code}")]
        public async Task<IActionResult> DeleteBook(string code)
        {
            var ok = await _books.DeleteByCodeAsync(code);
            return ok ? NoContent() : NotFound();
        }

        // GET /api/Admin/books/{code}
        [HttpGet("books/{code}")]
        public async Task<IActionResult> GetBookDetail(string code)
        {
            var b = await _books.GetByCodeAsync(code);
            return b is null ? NotFound() : Ok(b);
        }

        // INVENTORY: nh?p kho
        // POST /api/Admin/books/{code}/stock-in
        [HttpPost("books/{code}/stock-in")]
        public async Task<IActionResult> StockIn(string code, [FromBody] StockInDto dto)
        {
            if (dto is null || dto.Quantity <= 0)
                return BadRequest(new { error = "Quantity must be > 0" });

            var ok = await _books.IncreaseStockAsync(code, dto.Quantity);
            return ok ? NoContent() : NotFound(new { error = "Book not found" });
        }

        // REPORTS
        // GET /api/Admin/reports/top-books?limit=10
        [HttpGet("reports/top-books")]
        public async Task<IActionResult> TopBooks([FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);
            var items = await _books.GetTopSoldAsync(limit);
            return Ok(items);
        }

        // GET /api/Admin/reports/revenue?year=2025&month=10
        [HttpGet("reports/revenue")]
        public async Task<IActionResult> Revenue([FromQuery] int? year, [FromQuery] int month)
        {
            if (month is < 1 or > 12) return BadRequest(new { error = "Invalid month" });
            var y = year ?? DateTime.UtcNow.Year;
            var total = await new StatsService(HttpContext.RequestServices.GetRequiredService<MongoDbContext>(), _books)
                .GetRevenueAsync(y, month);
            return Ok(new { year = y, month, total });
        }
    }
}