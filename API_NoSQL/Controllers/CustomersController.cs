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

        public CustomersController(CustomerService service) => _service = service;

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
                Account = new Account
                {
                    Username = dto.Username,
                    Role = string.IsNullOrWhiteSpace(dto.Role) ? "khachhang" : dto.Role!
                },
                Orders = new List<Order>()
            };

            await _service.CreateAsync(c, dto.Password);
            c.Account.PasswordHash = string.Empty;
            return CreatedAtAction(nameof(GetByCode), new { code = c.Code }, c);
        }

        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, [FromBody] CustomerUpdateDto dto)
        {
            var ok = await _service.UpdateAsync(code, c =>
            {
                if (dto.FullName is not null) c.FullName = dto.FullName;
                if (dto.Phone is not null) c.Phone = dto.Phone;
                if (dto.Email is not null) c.Email = dto.Email;
                if (dto.Address is not null) c.Address = dto.Address;
            });

            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var ok = await _service.DeleteAsync(code);
            return ok ? NoContent() : NotFound();
        }
    }
}