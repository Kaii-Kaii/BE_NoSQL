using API_NoSQL.Dtos;
using API_NoSQL.Models;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class OrderService
    {
        private readonly MongoDbContext _ctx;
        private readonly BookService _books;
        private readonly CustomerService _customers;

        public OrderService(MongoDbContext ctx, BookService books, CustomerService customers)
        {
            _ctx = ctx;
            _books = books;
            _customers = customers;
        }

        private static string NewOrderCode() =>
            $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        public async Task<(bool Ok, string? Error, string? OrderCode)> CreateAsync(CreateOrderDto req)
        {
            var customer = await _customers.GetByCodeAsync(req.CustomerCode);
            if (customer is null) return (false, "Customer not found", null);
            if (req.Items is null || req.Items.Count == 0) return (false, "Order has no items", null);

            var items = new List<OrderItem>();
            var total = 0;

            foreach (var i in req.Items)
            {
                var bk = await _books.GetByCodeAsync(i.BookCode);
                if (bk is null) return (false, $"Book not found: {i.BookCode}", null);
                if (i.Quantity <= 0) return (false, $"Invalid quantity for {i.BookCode}", null);

                items.Add(new OrderItem
                {
                    BookCode = bk.Code,
                    BookName = bk.Name,
                    Quantity = i.Quantity,
                    UnitPrice = bk.Price,
                    LineTotal = bk.Price * i.Quantity
                });

                total += bk.Price * i.Quantity;
            }

            foreach (var i in items)
            {
                var ok = await _books.AdjustStockAndSoldAsync(i.BookCode, i.Quantity);
                if (!ok) return (false, $"Insufficient stock for {i.BookCode}", null);
            }

            var order = new Order
            {
                Code = NewOrderCode(),
                CreatedAt = DateTime.UtcNow,
                Total = total,
                Status = "Chua thanh toan",
                Items = items
            };

            customer.Orders.Add(order);
            await _customers.UpdateAsync(customer.Code, c => c.Orders = customer.Orders);

            return (true, null, order.Code);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(string customerCode)
        {
            var c = await _customers.GetByCodeAsync(customerCode);
            return c?.Orders?.OrderByDescending(o => o.CreatedAt).ToList() ?? [];
        }

        public async Task<Order?> GetOrderByCodeAsync(string orderCode)
        {
            var customers = await _ctx.Customers.Find(Builders<Customer>.Filter.Empty).ToListAsync();
            foreach (var c in customers)
            {
                var found = c.Orders.FirstOrDefault(o => o.Code == orderCode);
                if (found != null) return found;
            }
            return null;
        }

        // NEW: admin - list all orders (flattened)
        public async Task<(IReadOnlyList<AdminOrderListItemDto> Items, int Total)> GetAllOrdersAsync(int page, int pageSize)
        {
            var customers = await _ctx.Customers
                .Find(Builders<Customer>.Filter.Empty)
                .Project(c => new { c.Code, c.FullName, c.Orders })
                .ToListAsync();

            var flat = customers
                .SelectMany(c => (c.Orders ?? new List<Order>())
                    .Select(o => new AdminOrderListItemDto(
                        o.Code, c.Code, c.FullName, o.CreatedAt, o.Total, o.Status)))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var total = flat.Count;
            var items = flat.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }
    }
}