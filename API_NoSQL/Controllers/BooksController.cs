using API_NoSQL.Dtos;
using API_NoSQL.Models;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly BookService _service;

        public BooksController(BookService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? keyword,
            [FromQuery] string? categoryCode,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string? sortBy,
            [FromQuery] bool desc = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _service.SearchAsync(
                keyword, categoryCode, minPrice, maxPrice, sortBy, desc, page, pageSize);

            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _service.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("publishers")]
        public async Task<IActionResult> GetPublishers()
        {
            var publishers = await _service.GetAllPublishersAsync();
            return Ok(publishers);
        }

        [HttpGet("{code}")]
        public async Task<ActionResult<Book>> GetByCode(string code)
        {
            var b = await _service.GetByCodeAsync(code);
            return b is null ? NotFound() : Ok(b);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BookCreateDto dto)
        {
            // Tự động tạo mã nếu không có
            var bookCode = string.IsNullOrWhiteSpace(dto.Code) 
                ? _service.GenerateBookCode() 
                : dto.Code;

            // Kiểm tra mã đã tồn tại
            var existing = await _service.GetByCodeAsync(bookCode);
            if (existing is not null) 
                return Conflict(new { error = $"Mã sách {bookCode} đã tồn tại" });

            var b = new Book
            {
                Code = bookCode,
                Name = dto.Name,
                Author = dto.Author,
                PublishYear = dto.PublishYear,
                Price = dto.Price,
                InStock = dto.InStock,
                Description = dto.Description,
                CoverUrl = dto.CoverUrl,
                Status = dto.Status,
                Category = dto.Category,
                Publisher = dto.Publisher,
                Sold = 0
            };

            await _service.CreateAsync(b);
            return CreatedAtAction(nameof(GetByCode), new { code = b.Code }, b);
        }

        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, [FromBody] BookUpdateDto dto)
        {
            var ok = await _service.UpdateByCodeAsync(code, b =>
            {
                if (dto.Name is not null) b.Name = dto.Name;
                if (dto.Author is not null) b.Author = dto.Author;
                if (dto.PublishYear.HasValue) b.PublishYear = dto.PublishYear.Value;
                if (dto.Price.HasValue) b.Price = dto.Price.Value;
                if (dto.InStock.HasValue) b.InStock = dto.InStock.Value;
                if (dto.Description is not null) b.Description = dto.Description;
                if (dto.CoverUrl is not null) b.CoverUrl = dto.CoverUrl;
                if (dto.Status is not null) b.Status = dto.Status;
                if (dto.Category is not null) b.Category = dto.Category;
                if (dto.Publisher is not null) b.Publisher = dto.Publisher;
            });

            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var ok = await _service.DeleteByCodeAsync(code);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("top-categories")]
        public async Task<IActionResult> GetTopCategories([FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);
            var topCategories = await _service.GetTopCategoriesAsync(limit);
            return Ok(topCategories.Select(tc => new
            {
                CategoryCode = tc.CategoryCode ?? "Unknown",
                CategoryName = tc.CategoryName ?? "Unknown",
                TotalSold = tc.TotalSold
            }));
        }
    }
}