namespace API_NoSQL.Dtos
{
    public record OrderItemCreateDto(string BookCode, int Quantity);

    public record CreateOrderDto(
        string CustomerCode, 
        IList<OrderItemCreateDto> Items,
        string PaymentMethod); // "Tiền mặt" or "Chuyển khoản"

    // NEW: Update order status (admin)
    public record UpdateOrderStatusDto(string Status);

    // NEW: Cancel order DTO - Bắt buộc phải có lý do
    public record CancelOrderDto(string Reason);
}