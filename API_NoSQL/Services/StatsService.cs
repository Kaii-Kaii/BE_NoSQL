using API_NoSQL.Models;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class StatsService
    {
        private readonly MongoDbContext _ctx;
        private readonly BookService _books;

        public StatsService(MongoDbContext ctx, BookService books)
        {
            _ctx = ctx;
            _books = books;
        }

        public Task<List<Book>> GetTopBooksAsync(int limit) =>
            _books.GetTopSoldAsync(limit);

        public async Task<int> GetRevenueAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var customers = await _ctx.Customers
                .Find(Builders<Customer>.Filter.Empty)
                .Project(c => new { c.Orders })
                .ToListAsync();

            var total = customers
                .SelectMany(x => x.Orders ?? new List<Order>())
                .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
                .Sum(o => o.Total);

            return total;
        }

        // NEW: daily revenue for a month
        public async Task<List<(int Day, int Total)>> GetDailyRevenueAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var days = DateTime.DaysInMonth(year, month);
            var totals = Enumerable.Repeat(0, days).ToArray();

            var customers = await _ctx.Customers
                .Find(Builders<Customer>.Filter.Empty)
                .Project(c => new { c.Orders })
                .ToListAsync();

            foreach (var o in customers.SelectMany(c => c.Orders ?? new List<Order>()))
            {
                if (o.CreatedAt >= start && o.CreatedAt < end)
                {
                    var d = o.CreatedAt.ToLocalTime().Day; // 1..days
                    totals[d - 1] += o.Total;
                }
            }

            var result = new List<(int Day, int Total)>();
            for (int d = 1; d <= days; d++) result.Add((d, totals[d - 1]));
            return result;
        }

        // NEW: daily sold quantity for a month
        public async Task<List<(int Day, int Quantity)>> GetDailyBooksSoldAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            var days = DateTime.DaysInMonth(year, month);
            var totals = Enumerable.Repeat(0, days).ToArray();

            var customers = await _ctx.Customers
                .Find(Builders<Customer>.Filter.Empty)
                .Project(c => new { c.Orders })
                .ToListAsync();

            foreach (var o in customers.SelectMany(c => c.Orders ?? new List<Order>()))
            {
                if (o.CreatedAt >= start && o.CreatedAt < end)
                {
                    var qty = o.Items?.Sum(i => i.Quantity) ?? 0;
                    var d = o.CreatedAt.ToLocalTime().Day;
                    totals[d - 1] += qty;
                }
            }

            var result = new List<(int Day, int Quantity)>();
            for (int d = 1; d <= days; d++) result.Add((d, totals[d - 1]));
            return result;
        }

        public async Task<List<Customer>> GetCustomersByRangeAsync(DateTime? fromDate, DateTime? toDate)
        {
            // Lấy toàn bộ khách hàng
            var customers = await _ctx.Customers
                .Find(Builders<Customer>.Filter.Empty)
                .ToListAsync();

            // Lọc theo đơn hàng
            var filtered = customers
                .Where(c =>
                    c.Orders != null &&
                    c.Orders.Any(o =>
                        (!fromDate.HasValue || o.CreatedAt >= fromDate.Value) &&
                        (!toDate.HasValue || o.CreatedAt <= toDate.Value)
                    )
                )
                .ToList();

            return filtered;
        }
    }
}