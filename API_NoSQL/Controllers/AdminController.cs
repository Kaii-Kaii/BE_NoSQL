using API_NoSQL.Dtos;
using API_NoSQL.Security;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RoleAuthorize("admin")] // use header: X-Role: admin
    public class AdminController : ControllerBase
    {
        private readonly OrderService _orders;

        public AdminController(OrderService orders) => _orders = orders;

        // GET /api/Admin/orders?page=1&pageSize=20
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var (items, total) = await _orders.GetAllOrdersAsync(page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }

        // PUT /api/Admin/orders/{customerCode}/{orderCode}/status
        [HttpPut("orders/{customerCode}/{orderCode}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string customerCode, string orderCode, [FromBody] UpdateOrderStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { error = "Status is required" });

            var ok = await _orders.UpdateOrderStatusAsync(customerCode, orderCode, dto.Status);
            if (!ok)
                return BadRequest(new { error = "Failed to update order status. Check customer code, order code, and valid status." });

            return NoContent();
        }
    }
}