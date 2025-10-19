namespace API_NoSQL.Dtos
{
    public record LoginDto(string Username, string Password);

    // NEW: registration payload
    public record RegisterDto(
        string FullName,
        string Phone,
        string Email,
        string Address,
        string Username,
        string Password);
}