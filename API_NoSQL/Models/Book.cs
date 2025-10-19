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
        public string Code { get; set; } = default!; // e.g., "GKTH6"

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
        public string? Status { get; set; } // e.g., "còn hàng"

        [BsonElement("soluongdaban")]
        public int Sold { get; set; }

        [BsonElement("loai")]
        public BookCategory? Category { get; set; }

        [BsonElement("nxb")]
        public Publisher? Publisher { get; set; }
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
}

// To fix CS0246, ensure the MongoDB.Driver NuGet package is installed in your project.
// In Visual Studio, right-click your project > Manage NuGet Packages > Search for "MongoDB.Driver" and install it.
// No code changes are needed in this file if the package is installed.
// If you have already installed the package and still see the error, try rebuilding your solution.