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

    // Use as [FromForm] to support optional file upload for avatar
    public class CustomerUpdateDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        // Optional avatar file
        public Microsoft.AspNetCore.Http.IFormFile? Avatar { get; set; }
        // Optional flag to remove existing avatar when true and no new file provided
        public bool? RemoveAvatar { get; set; }
    }
}