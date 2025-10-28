namespace API_NoSQL.Dtos
{
    public record StockInDto(int Quantity);

    // DTO cho nhập hàng
    public class ImportInvoiceDto
    {
        public List<ImportItemDto> Items { get; set; } = [];
        public string? Note { get; set; }
    }

    public class ImportItemDto
    {
        public string BookCode { get; set; } = default!;
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }  // ← THÊM GIÁ NHẬP
    }
}
