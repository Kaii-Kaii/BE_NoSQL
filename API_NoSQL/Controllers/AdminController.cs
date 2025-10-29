using API_NoSQL.Dtos;
using API_NoSQL.Security;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MongoDB.Bson.Serialization.Attributes;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RoleAuthorize("admin")]
    public class AdminController : ControllerBase
    {
        private readonly OrderService _orders;
        private readonly BookService _books;
        private readonly CloudinaryService _cloudinary;
        private readonly InventoryService _inventory;

        public AdminController(
            OrderService orders, 
            BookService books, 
            CloudinaryService cloudinary,
            InventoryService inventory)
        {
            _orders = orders;
            _books = books;
            _cloudinary = cloudinary;
            _inventory = inventory;
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
            // Tự động tạo mã nếu không có
            var bookCode = string.IsNullOrWhiteSpace(dto.Code) 
                ? _books.GenerateBookCode() 
                : dto.Code;

            // Kiểm tra mã đã tồn tại
            var existing = await _books.GetByCodeAsync(bookCode);
            if (existing is not null) 
                return Conflict(new { error = $"Mã sách {bookCode} đã tồn tại" });

            string? coverUrl = null;
            if (dto.Cover is not null && dto.Cover.Length > 0)
            {
                coverUrl = await _cloudinary.UploadImageAsync(dto.Cover);
            }

            var b = new Models.Book
            {
                Code = bookCode,
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

        // POST /api/Admin/inventory/import
        /// <summary>
        /// Admin nhập hàng - tạo phiếu nhập và cập nhật tồn kho
        /// </summary>
        [HttpPost("inventory/import")]
        public async Task<IActionResult> ImportInventory([FromBody] ImportInvoiceDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { error = "Items cannot be empty" });

            // Lấy username từ header
            var username = Request.Headers["X-Username"].ToString();
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { error = "X-Username header is required" });

            // Chuyển đổi DTO sang tuple (có cả UnitPrice)
            var items = dto.Items.Select(i => (i.BookCode, i.Quantity, i.UnitPrice)).ToList();

            var (ok, error, invoice) = await _inventory.CreateImportInvoiceAsync(username, items, dto.Note);

            if (!ok)
                return BadRequest(new { error });

            return Created($"/api/Admin/inventory/import/{invoice!.Code}", invoice);
        }

        // GET /api/Admin/inventory/imports
        /// <summary>
        /// Lấy danh sách phiếu nhập của admin đang đăng nhập
        /// </summary>
        [HttpGet("inventory/imports")]
        public async Task<IActionResult> GetImportInvoices()
        {
            var username = Request.Headers["X-Username"].ToString();
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { error = "X-Username header is required" });

            var invoices = await _inventory.GetImportInvoicesByAdminAsync(username);
            return Ok(invoices);
        }

        // GET /api/Admin/inventory/imports/{code}
        /// <summary>
        /// Lấy chi tiết 1 phiếu nhập
        /// </summary>
        [HttpGet("inventory/imports/{code}")]
        public async Task<IActionResult> GetImportInvoiceDetail(string code)
        {
            var username = Request.Headers["X-Username"].ToString();
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { error = "X-Username header is required" });

            var invoice = await _inventory.GetImportInvoiceByCodeAsync(username, code);
            if (invoice == null)
                return NotFound(new { error = "Import invoice not found" });

            return Ok(invoice);
        }

        // GET /api/Admin/inventory/imports/history?page=1&pageSize=20&fromDate=2025-01-01&toDate=2025-12-31
        /// <summary>
        /// Lấy lịch sử nhập hàng với phân pag và lọc theo ngày
        /// </summary>
        [HttpGet("inventory/imports/history")]
        public async Task<IActionResult> GetImportHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var username = Request.Headers["X-Username"].ToString();
            if (string.IsNullOrEmpty(username))
                return BadRequest(new { error = "X-Username header is required" });

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _inventory.GetImportHistoryAsync(username, page, pageSize, fromDate, toDate);
            return Ok(new { total, page, pageSize, items });
        }
    }

    [BsonIgnoreExtraElements] // ← THÊM dòng này để bỏ qua trường không có trong model
    public class Order
    {
        [BsonElement("mahd")]
        public string Code { get; set; } = default!;

        [BsonElement("ngaylap")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("tongtien")]
        public int Total { get; set; }

        [BsonElement("trangthai")]
        public string Status { get; set; } = "DaDatHang";

        [BsonElement("hinhthucthanhtoan")]
        public string PaymentMethod { get; set; } = "TienMat";

        [BsonElement("chitiet")]
        public List<OrderItem> Items { get; set; } = [];

        // NEW: Thông tin huỷ đơn
        [BsonElement("lydohuy")]
        [BsonIgnoreIfNull]
        public string? CancelReason { get; set; }

        // NEW: Thời gian hoàn thành HOẶC huỷ đơn (dùng chung)
        [BsonElement("thoigianhoanthanh")]
        [BsonIgnoreIfNull]
        public DateTime? CompletedAt { get; set; }
    }

    public class OrderItem
    {
        // Define properties as needed, for example:
        public string BookCode { get; set; } = default!;
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
    }
}