namespace API_NoSQL.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
        public string BooksCollection { get; set; } = "Sach";
        public string CustomersCollection { get; set; } = "KhachHang";
    }
}