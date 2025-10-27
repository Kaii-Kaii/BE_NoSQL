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
        public string Code { get; set; } = default!;

        [BsonElement("hoten")]
        public string FullName { get; set; } = default!;

        [BsonElement("sdt")]
        public string Phone { get; set; } = default!;

        [BsonElement("email")]
        public string Email { get; set; } = default!;

        [BsonElement("diachi")]
        public string Address { get; set; } = default!;

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
        public string Role { get; set; } = "khachhang";

        // NEW: Trạng thái xác minh email
        [BsonElement("trangthai")]
        public string Status { get; set; } = "ChuaXacMinh"; // "ChuaXacMinh" | "DaXacMinh"
    }

    public class Order
    {
        [BsonElement("mahd")]
        public string Code { get; set; } = default!;

        [BsonElement("ngaylap")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("tongtien")]
        public int Total { get; set; }

        [BsonElement("trangthai")]
        public string Status { get; set; } = "DaDatHang";

        [BsonElement("hinhthucthanhtoan")]
        public string PaymentMethod { get; set; } = "TienMat";

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