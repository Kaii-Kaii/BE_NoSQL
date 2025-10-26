using API_NoSQL.Dtos;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly GoogleAuthService _googleAuth;

        public GoogleAuthController(GoogleAuthService googleAuth)
        {
            _googleAuth = googleAuth;
        }

        // POST /api/GoogleAuth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] GoogleLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
                return BadRequest(new { error = "Missing idToken" });

            var (ok, error, customer) = await _googleAuth.LoginWithGoogleAsync(dto.IdToken);
            if (!ok) return Unauthorized(new { error });

            return Ok(new
            {
                customer!.Code,
                customer.FullName,
                customer.Account.Username,
                customer.Account.Role,
                customer.Avatar
            });
        }
    }
}
