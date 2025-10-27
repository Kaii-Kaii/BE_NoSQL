namespace API_NoSQL.Dtos
{
    // Email/Password login (email = username)
    public record EmailLoginDto(string Email, string Password);

    // Email/Password registration (email = username)
    public record EmailRegisterDto(
        string Email,
        string Password,
        string FullName,
        string Phone,
        string Address);

    // Request password reset email
    public record PasswordResetRequestDto(string Email);

    // Request email change
    public record ChangeEmailRequestDto(string CustomerCode, string IdToken, string NewEmail);

    // Resend verification email
    public record ResendVerificationDto(string IdToken);

    // Confirm email change and update database
    public record ConfirmEmailChangeDto(string IdToken);
}