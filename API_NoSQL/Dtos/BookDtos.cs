using API_NoSQL.Models;

namespace API_NoSQL.Dtos
{
    public record BookCreateDto(
        string Code,
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
}