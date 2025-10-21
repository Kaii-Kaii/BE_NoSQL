using API_NoSQL.Dtos;
using API_NoSQL.Models;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly CustomerService _customers;

        public AuthController(AuthService auth, CustomerService customers)
        {
            _auth = auth;
            _customers = customers;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var (ok, error, customer) = await _auth.LoginAsync(dto);
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var byUsername = await _customers.GetByUsernameAsync(dto.Username);
            if (byUsername is not null)
                return Conflict(new { error = $"Username {dto.Username} already taken." });

            static string NewCustomerCode() => $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            var c = new Customer
            {
                Code = NewCustomerCode(),
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Avatar = null,
                Account = new Account
                {
                    Username = dto.Username,
                    Role = "khachhang"
                },
                Orders = new List<Order>()
            };

            await _customers.CreateAsync(c, dto.Password);
            c.Account.PasswordHash = string.Empty;

            return CreatedAtAction(
                actionName: "GetByCode",
                controllerName: "Customers",
                routeValues: new { code = c.Code },
                value: new { c.Code, c.FullName, c.Account.Username, c.Account.Role, c.Avatar });
        }

        // NEW: đổi mật khẩu
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.OldPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { error = "Invalid payload" });

            var (ok, error) = await _customers.ChangePasswordAsync(dto.Username, dto.OldPassword, dto.NewPassword);
            if (!ok)
            {
                if (error == "User not found") return NotFound(new { error });
                return BadRequest(new { error });
            }

            return NoContent();
        }
    }
}