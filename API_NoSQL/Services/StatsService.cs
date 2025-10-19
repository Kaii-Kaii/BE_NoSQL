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
    }
}