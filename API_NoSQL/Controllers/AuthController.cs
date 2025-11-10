using API_NoSQL.Dtos;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly FirebaseEmailAuthService _firebaseAuth;

        public AuthController(FirebaseEmailAuthService firebaseAuth)
        {
            _firebaseAuth = firebaseAuth;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmailLoginDto dto)
        {
            var (ok, error, customer) = await _firebaseAuth.LoginAsync(dto);
            if (!ok) return Unauthorized(new { error });
            return Ok(new
            {
                customer!.Code,
                customer.FullName,
                customer.Email,
                customer.Account.Username,
                customer.Account.Role,
                customer.Account.Status,
                customer.Avatar
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] EmailRegisterDto dto)
        {
            var (ok, error, customer) = await _firebaseAuth.RegisterAsync(dto);
            if (!ok) return BadRequest(new { error });

            return CreatedAtAction(
                actionName: "GetByCode",
                controllerName: "Customers",
                routeValues: new { code = customer!.Code },
                value: new 
                { 
                    customer.Code, 
                    customer.FullName, 
                    customer.Email,
                    customer.Account.Username,
                    customer.Account.Role,
                    customer.Account.Status,
                    customer.Avatar,
                    message = "Registration successful. Please check your email to verify your account."
                });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequestDto dto)
        {
            var sent = await _firebaseAuth.SendPasswordResetEmailAsync(dto.Email);
            if (!sent) return BadRequest(new { error = "Failed to send password reset email." });
            
            return Ok(new { message = "Password reset email sent. Please check your inbox." });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto)
        {
            var sent = await _firebaseAuth.SendVerificationEmailAsync(dto.IdToken);
            if (!sent) return BadRequest(new { error = "Failed to send verification email." });
            
            return Ok(new { message = "Verification email sent. Please check your inbox." });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (dto.CurrentPassword == dto.NewPassword)
            {
                return BadRequest(new { error = "New password must be different from current password" });
            }

            var (ok, error) = await _firebaseAuth.ChangePasswordAsync(dto);
            if (!ok) return BadRequest(new { error });
            
            return Ok(new { message = "Password changed successfully. Please login again with new password." });
        }
    }
}