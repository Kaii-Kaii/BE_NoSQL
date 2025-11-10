namespace API_NoSQL.Models.Emails;

public class OrderItemDto
{
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public int Price { get; set; }
    public int Subtotal { get; set; }
}

public class OrderEmailDto
{
    public string CustomerName { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string ShippingAddress { get; set; } = default!;

    public string OrderId { get; set; } = default!;
    public string OrderDate { get; set; } = default!;
    public List<OrderItemDto> Items { get; set; } = new();
    public int Subtotal { get; set; }
    public int ShippingFee { get; set; }
    public int Tax { get; set; }
    public int Total { get; set; }
    public string Status { get; set; } = default!;
}