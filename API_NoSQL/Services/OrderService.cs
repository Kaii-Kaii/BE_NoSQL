using API_NoSQL.Dtos;
using API_NoSQL.Models;
using API_NoSQL.Models.Emails;
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
        private readonly ISendGridEmailService _sendGridService;

        public OrderService(MongoDbContext ctx, BookService books, CustomerService customers, EmailSettings emailSettings, ISendGridEmailService sendGridService)
        {
            _ctx = ctx;
            _books = books;
            _customers = customers;
            _emailSettings = emailSettings;
            _sendGridService = sendGridService;
        }

        private static string NewOrderCode() =>
            $"HD{DateTime.UtcNow:yyyyMMddHHmmssfff}";

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

            var orderEmailData = new OrderEmailDto
            {
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email,
                PhoneNumber = customer.Phone,
                ShippingAddress = customer.Address,
                OrderId = order.Code,
                OrderDate = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductName = i.BookName,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice,
                    Subtotal = i.LineTotal
                }).ToList(),
                Subtotal = order.Total,
                ShippingFee = 0,
                Tax = 0,
                Total = order.Total,
                Status = order.Status
            };

            await _sendGridService.SendOrderConfirmationAsync(
                customer.Email,
                customer.FullName,
                orderEmailData);

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

        public async Task<bool> UpdateOrderStatusAsync(string customerCode, string orderCode, string newStatus)
        {
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

        public async Task<bool> ConfirmOrderReceivedAsync(string customerCode, string orderCode)
        {
            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer is null) return false;

            var order = customer.Orders.FirstOrDefault(o => o.Code == orderCode);
            if (order is null) return false;

            if (order.Status != "DangGiao")
                return false;

            order.Status = "HoanThanh";
            order.CompletedAt = DateTime.UtcNow;
            return await _customers.UpdateAsync(customerCode, c => c.Orders = customer.Orders);
        }

        public async Task<(bool Ok, string? Error)> CancelOrderAsync(string customerCode, string orderCode, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return (false, "Vui lòng nhập lý do huỷ đơn hàng");

            var customer = await _customers.GetByCodeAsync(customerCode);
            if (customer is null) 
                return (false, "Không tìm thấy khách hàng");

            var order = customer.Orders.FirstOrDefault(o => o.Code == orderCode);
            if (order is null) 
                return (false, "Không tìm thấy đơn hàng");

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

            foreach (var item in order.Items)
            {
                await _books.AdjustStockAndSoldAsync(item.BookCode, -item.Quantity);
            }

            order.Status = "DaHuy";
            order.CancelReason = reason;
            order.CompletedAt = DateTime.UtcNow;
            
            var success = await _customers.UpdateAsync(customerCode, c => c.Orders = customer.Orders);
            
            if (success)
                return (true, null);
            
            return (false, "Không thể cập nhật trạng thái đơn hàng");
        }
    }
}