using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API_NoSQL.Models
{
    public class Customer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("makh")]
        public string Code { get; set; } = default!; // e.g., "KH001"

        [BsonElement("hoten")]
        public string FullName { get; set; } = default!;

        [BsonElement("sdt")]
        public string Phone { get; set; } = default!;

        [BsonElement("email")]
        public string Email { get; set; } = default!;

        [BsonElement("diachi")]
        public string Address { get; set; } = default!;

        // NEW: Avatar URL, nullable. Not stored until uploaded.
        [BsonElement("avatar")]
        [BsonIgnoreIfNull]
        public string? Avatar { get; set; }

        [BsonElement("taikhoan")]
        public Account Account { get; set; } = new();

        [BsonElement("hoadon")]
        public List<Order> Orders { get; set; } = [];
    }

    public class Account
    {
        [BsonElement("tendangnhap")]
        public string Username { get; set; } = default!;

        [BsonElement("matkhau")]
        public string PasswordHash { get; set; } = default!;

        [BsonElement("vaitro")]
        public string Role { get; set; } = "khachhang"; // or "admin"
    }

    public class Order
    {
        [BsonElement("mahd")]
        public string Code { get; set; } = default!; // e.g., "HD001"

        [BsonElement("ngaylap")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("tongtien")]
        public int Total { get; set; }

        // NEW: Order status workflow
        // "Ðã d?t hàng" -> "Ðang giao" -> "Hoàn thành"
        [BsonElement("trangthai")]
        public string Status { get; set; } = "Ðã d?t hàng";

        // NEW: Payment method - "Ti?n m?t" or "Chuy?n kho?n"
        [BsonElement("hinhthucthanhtoan")]
        public string PaymentMethod { get; set; } = "Ti?n m?t";

        [BsonElement("chitiet")]
        public List<OrderItem> Items { get; set; } = [];
    }

    public class OrderItem
    {
        [BsonElement("masp")]
        public string BookCode { get; set; } = default!;

        [BsonElement("tensp")]
        public string BookName { get; set; } = default!;

        [BsonElement("soluong")]
        public int Quantity { get; set; }

        [BsonElement("dongia")]
        public int UnitPrice { get; set; }

        [BsonElement("thanhtien")]
        public int LineTotal { get; set; }
    }
}