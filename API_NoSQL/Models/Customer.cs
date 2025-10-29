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

        // Danh sách phiếu nhập hàng cho admin
        [BsonElement("phieunhap")]
        [BsonIgnoreIfNull]
        public List<ImportInvoice>? ImportInvoices { get; set; }
    }

    public class Account
    {
        [BsonElement("tendangnhap")]
        public string Username { get; set; } = default!;

        [BsonElement("matkhau")]
        public string PasswordHash { get; set; } = default!;

        [BsonElement("vaitro")]
        public string Role { get; set; } = "khachhang";

        [BsonElement("trangthai")]
        public string Status { get; set; } = "ChuaXacMinh";
    }

    [BsonIgnoreExtraElements] // ← THÊM dòng này
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

        // NEW: Thông tin huỷ đơn
        [BsonElement("lydohuy")]
        [BsonIgnoreIfNull]
        public string? CancelReason { get; set; }

        // NEW: Thời gian hoàn thành HOẶC huỷ đơn (dùng chung)
        [BsonElement("thoigianhoanthanh")]
        [BsonIgnoreIfNull]
        public DateTime? CompletedAt { get; set; }
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

    // Model cho phiếu nhập hàng
    public class ImportInvoice
    {
        [BsonElement("mapn")]
        public string Code { get; set; } = default!;

        [BsonElement("ngaynhap")]
        public DateTime ImportDate { get; set; }

        [BsonElement("tongsoluong")]
        public int TotalQuantity { get; set; }

        [BsonElement("tongtien")]
        public int TotalAmount { get; set; }

        [BsonElement("ghichu")]
        public string? Note { get; set; }

        [BsonElement("chitiet")]
        public List<ImportItem> Items { get; set; } = [];
    }

    public class ImportItem
    {
        [BsonElement("masp")]
        public string BookCode { get; set; } = default!;

        [BsonElement("tensp")]
        public string BookName { get; set; } = default!;

        [BsonElement("soluong")]
        public int Quantity { get; set; }

        [BsonElement("dongianhap")]
        public int UnitPrice { get; set; }

        [BsonElement("thanhtien")]
        public int LineTotal { get; set; }
    }
}