namespace API_NoSQL.Dtos
{
    public record OrderItemCreateDto(string BookCode, int Quantity);

    public record CreateOrderDto(
        string CustomerCode, 
        IList<OrderItemCreateDto> Items,
        string PaymentMethod); // "Ti?n m?t" or "Chuy?n kho?n"

    // NEW: Update order status (admin)
    public record UpdateOrderStatusDto(string Status);
}