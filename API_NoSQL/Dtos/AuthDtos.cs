namespace API_NoSQL.Dtos
{
    public record LoginDto(string Username, string Password);

    public record RegisterDto(
        string FullName,
        string Phone,
        string Email,
        string Address,
        string Username,
        string Password);

    // NEW: đổi mật khẩu
    public record ChangePasswordDto(
        string Username,
        string OldPassword,
        string NewPassword);
}