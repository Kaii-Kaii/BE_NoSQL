using API_NoSQL.Models;
using Microsoft.AspNetCore.Http;

namespace API_NoSQL.Dtos
{
    public record BookCreateDto(
        string? Code, // ← Thay đổi thành nullable
        string Name,
        string Author,
        int PublishYear,
        int Price,
        int InStock,
        string? Description,
        string? CoverUrl,
        string? Status,
        BookCategory? Category,
        Publisher? Publisher);

    public record BookUpdateDto(
        string? Name,
        string? Author,
        int? PublishYear,
        int? Price,
        int? InStock,
        string? Description,
        string? CoverUrl,
        string? Status,
        BookCategory? Category,
        Publisher? Publisher);

    // NEW: Admin create book via form-data with image upload
    public class AdminBookCreateFormDto
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Author { get; set; } = default!;
        public int PublishYear { get; set; }
        public int Price { get; set; }
        public int InStock { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public BookCategory? Category { get; set; }
        public Publisher? Publisher { get; set; }
        public IFormFile? Cover { get; set; }
    }

    // NEW: Admin update book via form-data with optional image upload
    public class AdminBookUpdateFormDto
    {
        public string? Name { get; set; }
        public string? Author { get; set; }
        public int? PublishYear { get; set; }
        public int? Price { get; set; }
        public int? InStock { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public BookCategory? Category { get; set; }
        public Publisher? Publisher { get; set; }
        public IFormFile? Cover { get; set; }
    }
}