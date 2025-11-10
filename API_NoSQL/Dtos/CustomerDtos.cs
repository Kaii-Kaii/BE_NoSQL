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

    public class CustomerUpdateDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Avatar { get; set; }
        public bool? RemoveAvatar { get; set; }
    }
}