using API_NoSQL.Models;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class InventoryService
    {
        private readonly MongoDbContext _ctx;
        private readonly BookService _bookService;

        public InventoryService(MongoDbContext ctx, BookService bookService)
        {
            _ctx = ctx;
            _bookService = bookService;
        }

        /// <summary>
        /// Tạo phiếu nhập hàng cho admin - lưu sách, số lượng và giá nhập
        /// </summary>
        public async Task<(bool Ok, string? Error, ImportInvoice? Invoice)> CreateImportInvoiceAsync(
            string adminUsername, 
            List<(string BookCode, int Quantity, int UnitPrice)> items,  // ← THÊM UnitPrice
            string? note = null)
        {
            try
            {
                // 1. Lấy thông tin admin
                var admin = await _ctx.Customers
                    .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                    .FirstOrDefaultAsync();

                if (admin == null)
                    return (false, "Admin not found", null);

                // 2. Tạo mã phiếu nhập
                var importCode = $"PN{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                // 3. Xử lý từng item
                var importItems = new List<ImportItem>();
                var totalQuantity = 0;
                var totalAmount = 0;  // ← THÊM TỔNG TIỀN

                foreach (var (bookCode, quantity, unitPrice) in items)
                {
                    if (quantity <= 0)
                        return (false, $"Quantity for book {bookCode} must be > 0", null);

                    if (unitPrice < 0)  // ← VALIDATE GIÁ NHẬP
                        return (false, $"Unit price for book {bookCode} must be >= 0", null);

                    // Lấy thông tin sách
                    var book = await _bookService.GetByCodeAsync(bookCode);
                    if (book == null)
                        return (false, $"Book {bookCode} not found", null);

                    // Tăng tồn kho
                    await _bookService.IncreaseStockAsync(bookCode, quantity);

                    // Tính tổng
                    totalQuantity += quantity;
                    var lineTotal = quantity * unitPrice;
                    totalAmount += lineTotal;

                    // Thêm vào danh sách item
                    importItems.Add(new ImportItem
                    {
                        BookCode = bookCode,
                        BookName = book.Name,
                        Quantity = quantity,
                        UnitPrice = unitPrice,      // ← LƯU GIÁ NHẬP
                        LineTotal = lineTotal        // ← LƯU THÀNH TIỀN
                    });
                }

                // 4. Tạo phiếu nhập
                var invoice = new ImportInvoice
                {
                    Code = importCode,
                    ImportDate = DateTime.UtcNow,
                    TotalQuantity = totalQuantity,
                    TotalAmount = totalAmount,  // ← LƯU TỔNG TIỀN
                    Note = note,
                    Items = importItems
                };

                // 5. Lưu vào admin account
                var update = Builders<Customer>.Update.Push(c => c.ImportInvoices, invoice);
                var result = await _ctx.Customers.UpdateOneAsync(
                    c => c.Code == admin.Code,
                    update
                );

                if (result.ModifiedCount == 0)
                    return (false, "Failed to save import invoice", null);

                return (true, null, invoice);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// L?y danh sách phi?u nh?p c?a admin
        /// </summary>
        public async Task<List<ImportInvoice>> GetImportInvoicesByAdminAsync(string adminUsername)
        {
            var admin = await _ctx.Customers
                .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                .FirstOrDefaultAsync();

            return admin?.ImportInvoices ?? [];
        }

        /// <summary>
        /// L?y chi ti?t 1 phi?u nh?p
        /// </summary>
        public async Task<ImportInvoice?> GetImportInvoiceByCodeAsync(string adminUsername, string invoiceCode)
        {
            var admin = await _ctx.Customers
                .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                .FirstOrDefaultAsync();

            return admin?.ImportInvoices?.FirstOrDefault(i => i.Code == invoiceCode);
        }

        /// <summary>
        /// L?y l?ch s? nh?p hàng v?i phân trang và l?c
        /// </summary>
        public async Task<(List<ImportInvoice> Items, int Total)> GetImportHistoryAsync(
            string adminUsername,
            int page,
            int pageSize,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var admin = await _ctx.Customers
                .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                .FirstOrDefaultAsync();

            if (admin?.ImportInvoices == null)
                return ([], 0);

            var query = admin.ImportInvoices.AsQueryable();

            // L?c theo kho?ng th?i gian
            if (fromDate.HasValue)
                query = query.Where(i => i.ImportDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.ImportDate <= toDate.Value);

            // S?p x?p theo ngày m?i nh?t
            var sorted = query.OrderByDescending(i => i.ImportDate);

            var total = sorted.Count();
            var items = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, total);
        }
    }
}