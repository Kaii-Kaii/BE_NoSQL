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
        public async Task<(bool Ok, string? Error, ImportInvoice? Invoice)> CreateImportInvoiceAsync(
            string adminUsername, 
            List<(string BookCode, int Quantity, int UnitPrice)> items,
            string? note = null)
        {
            try
            {
                var admin = await _ctx.Customers
                    .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                    .FirstOrDefaultAsync();

                if (admin == null)
                    return (false, "Admin not found", null);

                var importCode = $"PN{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                var importItems = new List<ImportItem>();
                var totalQuantity = 0;
                var totalAmount = 0;

                foreach (var (bookCode, quantity, unitPrice) in items)
                {
                    if (quantity <= 0)
                        return (false, $"Quantity for book {bookCode} must be > 0", null);

                    if (unitPrice < 0)
                        return (false, $"Unit price for book {bookCode} must be >= 0", null);

                    var book = await _bookService.GetByCodeAsync(bookCode);
                    if (book == null)
                        return (false, $"Book {bookCode} not found", null);

                    await _bookService.IncreaseStockAsync(bookCode, quantity);

                    totalQuantity += quantity;
                    var lineTotal = quantity * unitPrice;
                    totalAmount += lineTotal;

                    importItems.Add(new ImportItem
                    {
                        BookCode = bookCode,
                        BookName = book.Name,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal
                    });
                }

                var invoice = new ImportInvoice
                {
                    Code = importCode,
                    ImportDate = DateTime.UtcNow,
                    TotalQuantity = totalQuantity,
                    TotalAmount = totalAmount,
                    Note = note,
                    Items = importItems
                };

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

        public async Task<List<ImportInvoice>> GetImportInvoicesByAdminAsync(string adminUsername)
        {
            var admin = await _ctx.Customers
                .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                .FirstOrDefaultAsync();

            return admin?.ImportInvoices ?? [];
        }

        public async Task<ImportInvoice?> GetImportInvoiceByCodeAsync(string adminUsername, string invoiceCode)
        {
            var admin = await _ctx.Customers
                .Find(c => c.Account.Username == adminUsername && c.Account.Role == "admin")
                .FirstOrDefaultAsync();

            return admin?.ImportInvoices?.FirstOrDefault(i => i.Code == invoiceCode);
        }

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

            if (fromDate.HasValue)
                query = query.Where(i => i.ImportDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.ImportDate <= toDate.Value);

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