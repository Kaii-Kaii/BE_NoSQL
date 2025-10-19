namespace API_NoSQL.Dtos
{
    public record OrderItemCreateDto(string BookCode, int Quantity);

    public record CreateOrderDto(string CustomerCode, IList<OrderItemCreateDto> Items);
}