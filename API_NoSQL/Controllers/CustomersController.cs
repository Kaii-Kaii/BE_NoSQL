using API_NoSQL.Dtos;
using API_NoSQL.Models;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _service;
        private readonly CloudinaryService _cloudinary;

        public CustomersController(CustomerService service, CloudinaryService cloudinary)
        {
            _service = service;
            _cloudinary = cloudinary;
        }

        [HttpGet("{code}")]
        public async Task<ActionResult<Customer>> GetByCode(string code)
        {
            var c = await _service.GetByCodeAsync(code);
            if (c is null) return NotFound();
            c.Account.PasswordHash = string.Empty;
            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            var existing = await _service.GetByCodeAsync(dto.Code);
            if (existing is not null) return Conflict($"Customer code {dto.Code} already exists.");

            var byUsername = await _service.GetByUsernameAsync(dto.Username);
            if (byUsername is not null) return Conflict($"Username {dto.Username} already taken.");

            var c = new Customer
            {
                Code = dto.Code,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Avatar = null,
                Account = new Account
                {
                    Username = dto.Username,
                    Role = string.IsNullOrWhiteSpace(dto.Role) ? "khachhang" : dto.Role!
                },
                Orders = new List<API_NoSQL.Models.Order>()
            };

            await _service.CreateAsync(c, dto.Password);
            c.Account.PasswordHash = string.Empty;
            return CreatedAtAction(nameof(GetByCode), new { code = c.Code }, c);
        }

        [HttpPut("{code}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(string code, [FromForm] CustomerUpdateDto dto)
        {
            // Basic field updates; ignore empty strings from Swagger
            var updated = await _service.UpdateAsync(code, c =>
            {
                if (!string.IsNullOrWhiteSpace(dto.FullName)) c.FullName = dto.FullName!;
                if (!string.IsNullOrWhiteSpace(dto.Phone)) c.Phone = dto.Phone!;
                if (!string.IsNullOrWhiteSpace(dto.Email)) c.Email = dto.Email!;
                if (!string.IsNullOrWhiteSpace(dto.Address)) c.Address = dto.Address!;
            });

            if (!updated) return NotFound();

            // Avatar handling
            if (dto.Avatar is not null && dto.Avatar.Length > 0)
            {
                var url = await _cloudinary.UploadImageAsync(dto.Avatar);
                await _service.SetAvatarUrlByCodeAsync(code, url);
            }
            else if (dto.RemoveAvatar == true)
            {
                await _service.RemoveAvatarByCodeAsync(code);
            }

            return NoContent();
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var ok = await _service.DeleteAsync(code);
            return ok ? NoContent() : NotFound();
        }

        // NEW: Upload avatar and save URL (still available if you need a dedicated endpoint)
        [HttpPost("{code}/avatar")]
        public async Task<IActionResult> UploadAvatar(string code, IFormFile file)
        {
            if (file is null) return BadRequest(new { error = "Missing file" });

            var customer = await _service.GetByCodeAsync(code);
            if (customer is null) return NotFound(new { error = "Customer not found" });

            var url = await _cloudinary.UploadImageAsync(file);
            var ok = await _service.SetAvatarUrlByCodeAsync(code, url);
            if (!ok) return StatusCode(500, new { error = "Failed to save avatar" });

            return Ok(new { avatar = url });
        }
    }

    // NEW: Admin create book via form-data with image upload
    public class AdminBookCreateFormDto
    {
        public string? Code { get; set; } // ← ĐỔI THÀNH NULLABLE
        public string Name { get; set; } = default!;
        public string Author { get; set; } = default!;
        public int PublishYear { get; set; }
        public int Price { get; set; }
        public int InStock { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public BookCategory? Category { get; set; }
        public Publisher? Publisher { get; set; }
        public IFormFile? Cover { get; set; }
    }
}