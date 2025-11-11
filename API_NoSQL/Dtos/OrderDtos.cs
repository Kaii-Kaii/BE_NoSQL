namespace API_NoSQL.Dtos
{
    public record OrderItemCreateDto(string BookCode, int Quantity);

    public record CreateOrderDto(
        string CustomerCode,
        IList<OrderItemCreateDto> Items,
        string PaymentMethod); // "Tiền mặt" or "Chuyển khoản"

    public record UpdateOrderStatusDto(string Status);

    public record CancelOrderDto(string Reason);
}