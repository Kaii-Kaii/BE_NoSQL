namespace API_NoSQL.Dtos
{
    public record CustomerCreateDto(
        string Code,
        string FullName,
        string Phone,
        string Email,
        string Address,
        string Username,
        string Password,
        string? Role);

    public record CustomerUpdateDto(
        string? FullName,
        string? Phone,
        string? Email,
        string? Address);
}