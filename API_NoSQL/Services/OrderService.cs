using API_NoSQL.Dtos;
using API_NoSQL.Models;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;

namespace API_NoSQL.Services
{
    public class OrderService
    {
        private readonly MongoDbContext _ctx;
        private readonly BookService _books;
        private readonly CustomerService _customers;
        private readonly EmailSettings _emailSettings;
        public OrderService(MongoDbContext ctx, BookService books, CustomerService customers, EmailSettings emailSettings)
        {
            _ctx = ctx;
            _books = books;
            _customers = customers;
            _emailSettings = emailSettings;
        }

        private static string NewOrderCode() =>
            $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        // NEW: Normalize payment method (remove accents)
        private static string NormalizePaymentMethod(string method)
        {
            return method?.ToLower().Trim() switch
            {
                "tien mat" => "TienMat",
                "tienmat" => "TienMat",
                "chuyen khoan" => "ChuyenKhoan",
                "chuyenkhoan" => "ChuyenKhoan",
                _ => method
            };
        }

        public async Task<(bool Ok, string? Error, string? OrderCode)> CreateAsync(CreateOrderDto req)
        {
            var customer = await _customers.GetByCodeAsync(req.CustomerCode);
            if (customer is null) return (false, "Customer not found", null);
            if (req.Items is null || req.Items.Count == 0) return (false, "Order has no items", null);

            // Normalize and validate payment method
            var normalizedPaymentMethod = NormalizePaymentMethod(req.PaymentMethod);
            var validPaymentMethods = new[] { "TienMat", "ChuyenKhoan" };
            if (!validPaymentMethods.Contains(normalizedPaymentMethod))
                return (false, "Invalid payment method. Use 'TienMat' or 'ChuyenKhoan'", null);

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
                Status = "DaDatHang",
                PaymentMethod = normalizedPaymentMethod,
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
                        o.Code, c.Code, c.FullName, o.CreatedAt, o.Total, o.Status, o.PaymentMethod)))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var total = flat.Count;
            var items = flat.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }

        // NEW: Admin updates order status
        public async Task<bool> UpdateOrderStatusAsync(string customerCode, string orderCode, string newStatus)
        {
            // Accept only normalized statuses without accents - THÊM "DaHuy"
            var validStatuses = new[] { "DaDatHang", "DangGiao", "HoanThanh", "DaHuy" };
            if (!validStatuses.Contains(newStatus))
                return false;

            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer is null) return false;

            var order = customer.Orders.FirstOrDefault(o => o.Code == orderCode);
            if (order is null) return false;

            order.Status = newStatus;
            return await _customers.UpdateAsync(customerCode, c => c.Orders = customer.Orders);
        }

        // NEW: Customer confirms order received -> "HoanThanh"
        public async Task<bool> ConfirmOrderReceivedAsync(string customerCode, string orderCode)
        {
            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer is null) return false;

            var order = customer.Orders.FirstOrDefault(o => o.Code == orderCode);
            if (order is null) return false;

            // Only allow confirmation if status is "DangGiao"
            if (order.Status != "DangGiao")
                return false;

            order.Status = "HoanThanh";
            order.CompletedAt = DateTime.UtcNow; // Lưu thời gian hoàn thành
            return await _customers.UpdateAsync(customerCode, c => c.Orders = customer.Orders);
        }

        // NEW: Customer cancels order (only allowed for "DaDatHang" status)
        public async Task<(bool Ok, string? Error)> CancelOrderAsync(string customerCode, string orderCode, string reason)
        {
            // Validate reason
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Vui lòng nhập lý do huỷ đơn hàng");

            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer is null) 
                return (false, "Không tìm thấy khách hàng");

            var order = customer.Orders.FirstOrDefault(o => o.Code == orderCode);
            if (order is null) 
                return (false, "Không tìm thấy đơn hàng");

            // Only allow cancellation if status is "DaDatHang"
            if (order.Status != "DaDatHang")
            {
                if (order.Status == "DangGiao")
                    return (false, "Không thể huỷ đơn hàng đang được giao");
                if (order.Status == "HoanThanh")
                    return (false, "Không thể huỷ đơn hàng đã hoàn thành");
                if (order.Status == "DaHuy")
                    return (false, "Đơn hàng đã được huỷ trước đó");
                
                return (false, $"Không thể huỷ đơn hàng có trạng thái '{order.Status}'");
            }

            // Restore stock when order is cancelled
            foreach (var item in order.Items)
            {
                // Reverse the stock adjustment (add back quantity, subtract from sold)
                await _books.AdjustStockAndSoldAsync(item.BookCode, -item.Quantity);
            }

            // Update order status to cancelled with reason
            order.Status = "DaHuy";
            order.CancelReason = reason;
            order.CompletedAt = DateTime.UtcNow; // Dùng chung trường này cho thời gian huỷ
            
            var success = await _customers.UpdateAsync(customerCode, c => c.Orders = customer.Orders);
            
            if (success)
                return (true, null);
            
            return (false, "Không thể cập nhật trạng thái đơn hàng");
        }
    }
}