using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace API_NoSQL.Models
{
    public class Book
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("masp")]
        public string Code { get; set; } = default!;

        [BsonElement("tensp")]
        public string Name { get; set; } = default!;

        [BsonElement("tentacgia")]
        public string Author { get; set; } = default!;

        [BsonElement("namxb")]
        public int PublishYear { get; set; }

        [BsonElement("giaban")]
        public int Price { get; set; }

        [BsonElement("tonkho")]
        public int InStock { get; set; }

        [BsonElement("mota")]
        public string? Description { get; set; }

        [BsonElement("anhbia")]
        public string? CoverUrl { get; set; }

        [BsonElement("trangthai")]
        public string? Status { get; set; }

        [BsonElement("soluongdaban")]
        public int Sold { get; set; }

        [BsonElement("loai")]
        public BookCategory? Category { get; set; }

        [BsonElement("nxb")]
        public Publisher? Publisher { get; set; }

        [BsonElement("danhgia")]
        [BsonIgnoreIfNull]
        public List<BookReview>? Reviews { get; set; }

        [BsonElement("diemtrungbinh")]
        [BsonIgnoreIfDefault]
        public double AverageRating { get; set; }
    }

    public class BookCategory
    {
        [BsonElement("maloai")]
        public string? Code { get; set; }

        [BsonElement("tenloai")]
        public string? Name { get; set; }
    }

    public class Publisher
    {
        [BsonElement("manxb")]
        public string? Code { get; set; }

        [BsonElement("tennxb")]
        public string? Name { get; set; }

        [BsonElement("diachi")]
        public string? Address { get; set; }
    }

    public class BookReview
    {
        [BsonElement("madg")]
        public string ReviewId { get; set; } = default!;

        [BsonElement("makh")]
        public string CustomerCode { get; set; } = default!;

        [BsonElement("tennguoidanhgia")]
        public string ReviewerName { get; set; } = default!;

        [BsonElement("avatar")]
        [BsonIgnoreIfNull]
        public string? AvatarUrl { get; set; }

        [BsonElement("mahd")]
        public string OrderCode { get; set; } = default!;

        [BsonElement("sosao")]
        public int Rating { get; set; }

        [BsonElement("noidung")]
        public string Content { get; set; } = default!;

        [BsonElement("thoigian")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("capnhat")]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedAt { get; set; }
    }
}