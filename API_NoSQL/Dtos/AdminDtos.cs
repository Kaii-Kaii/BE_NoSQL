namespace API_NoSQL.Dtos
{
    public record AdminOrderListItemDto(
        string OrderCode,
        string CustomerCode,
        string CustomerName,
        DateTime CreatedAt,
        int Total,
        string Status);
}