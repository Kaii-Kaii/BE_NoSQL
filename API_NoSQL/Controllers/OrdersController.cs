using API_NoSQL.Dtos;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orders;

        public OrdersController(OrderService orders) => _orders = orders;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var (ok, error, orderCode) = await _orders.CreateAsync(dto);
            if (!ok) return BadRequest(new { error });
            return Ok(new { orderCode });
        }

        [HttpGet("by-customer/{customerCode}")]
        public async Task<IActionResult> ByCustomer(string customerCode)
        {
            var items = await _orders.GetOrdersByCustomerAsync(customerCode);
            return Ok(items);
        }

        [HttpGet("{orderCode}")]
        public async Task<IActionResult> Get(string orderCode)
        {
            var o = await _orders.GetOrderByCodeAsync(orderCode);
            return o is null ? NotFound() : Ok(o);
        }

        // NEW: Customer confirms order received -> "Hoàn thành"
        [HttpPut("{customerCode}/orders/{orderCode}/confirm")]
        public async Task<IActionResult> ConfirmReceived(string customerCode, string orderCode)
        {
            var ok = await _orders.ConfirmOrderReceivedAsync(customerCode, orderCode);
            if (!ok) return BadRequest(new { error = "Cannot confirm order. Either not found, already completed, or not in delivery status." });
            return NoContent();
        }

        
    }
}