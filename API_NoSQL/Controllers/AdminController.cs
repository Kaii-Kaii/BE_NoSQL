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
    }
}