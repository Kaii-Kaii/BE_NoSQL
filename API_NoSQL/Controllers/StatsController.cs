using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly StatsService _stats;

        public StatsController(StatsService stats) => _stats = stats;

        // GET /api/Stats/top-books?limit=10
        [HttpGet("top-books")]
        public async Task<IActionResult> TopBooks([FromQuery] int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 100);
            var items = await _stats.GetTopBooksAsync(limit);
            return Ok(items);
        }

        // GET /api/Stats/revenue?year=2025&month=10
        [HttpGet("revenue")]
        public async Task<IActionResult> Revenue([FromQuery] int? year, [FromQuery] int month)
        {
            if (month is < 1 or > 12) return BadRequest(new { error = "Invalid month" });
            var y = year ?? DateTime.UtcNow.Year;
            var total = await _stats.GetRevenueAsync(y, month);
            return Ok(new { year = y, month, total });
        }

        // GET /api/Stats/revenue/daily?year=2025&month=10
        [HttpGet("revenue/daily")]
        public async Task<IActionResult> RevenueDaily([FromQuery] int? year, [FromQuery] int month)
        {
            if (month is < 1 or > 12) return BadRequest(new { error = "Invalid month" });
            var y = year ?? DateTime.UtcNow.Year;
            var data = await _stats.GetDailyRevenueAsync(y, month);
            return Ok(new { year = y, month, points = data.Select(d => new { day = d.Day, total = d.Total }) });
        }

        // GET /api/Stats/books-sold/daily?year=2025&month=10
        [HttpGet("books-sold/daily")]
        public async Task<IActionResult> BooksSoldDaily([FromQuery] int? year, [FromQuery] int month)
        {
            if (month is < 1 or > 12) return BadRequest(new { error = "Invalid month" });
            var y = year ?? DateTime.UtcNow.Year;
            var data = await _stats.GetDailyBooksSoldAsync(y, month);
            return Ok(new { year = y, month, points = data.Select(d => new { day = d.Day, quantity = d.Quantity }) });
        }
    }
}