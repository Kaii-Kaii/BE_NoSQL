namespace API_NoSQL.Dtos
{
    public record EmailLoginDto(string Email, string Password);

    public record EmailRegisterDto(
        string Email,
        string Password,
        string FullName,
        string Phone,
        string Address);

    public record PasswordResetRequestDto(string Email);

    public record ResendVerificationDto(string IdToken);

    public record ConfirmEmailChangeDto(string IdToken);

    public record ChangePasswordDto(
        string Email,
        string CurrentPassword,
        string NewPassword);
}