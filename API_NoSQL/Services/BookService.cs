using API_NoSQL.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class BookService
    {
        private readonly MongoDbContext _ctx;

        public BookService(MongoDbContext ctx) => _ctx = ctx;

        public async Task<(IReadOnlyList<Book> Items, long Total)> SearchAsync(
            string? keyword, string? categoryCode, int? minPrice, int? maxPrice,
            string? sortBy, bool desc, int page = 1, int pageSize = 20)
        {
            var fb = Builders<Book>.Filter;
            var filter = fb.Empty;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(keyword, "i");
                filter &= fb.Or(
                    fb.Regex(b => b.Name, regex),
                    fb.Regex(b => b.Author, regex),
                    fb.Regex(b => b.Code, regex));
            }

            if (!string.IsNullOrWhiteSpace(categoryCode))
                filter &= fb.Eq("loai.maloai", categoryCode);

            if (minPrice.HasValue) filter &= fb.Gte(b => b.Price, minPrice);
            if (maxPrice.HasValue) filter &= fb.Lte(b => b.Price, maxPrice);

            var sortDef = sortBy?.ToLower() switch
            {
                "price" => desc ? Builders<Book>.Sort.Descending(b => b.Price) : Builders<Book>.Sort.Ascending(b => b.Price),
                "sold" => desc ? Builders<Book>.Sort.Descending(b => b.Sold) : Builders<Book>.Sort.Ascending(b => b.Sold),
                "name" => desc ? Builders<Book>.Sort.Descending(b => b.Name) : Builders<Book>.Sort.Ascending(b => b.Name),
                _ => Builders<Book>.Sort.Ascending(b => b.Name)
            };

            var total = await _ctx.Books.CountDocumentsAsync(filter);
            var items = await _ctx.Books.Find(filter)
                .Sort(sortDef)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<Book?> GetByCodeAsync(string code) =>
            _ctx.Books.Find(b => b.Code == code).FirstOrDefaultAsync();

        public async Task CreateAsync(Book book) =>
            await _ctx.Books.InsertOneAsync(book);

        public async Task<bool> UpdateByCodeAsync(string code, Action<Book> update)
        {
            var book = await GetByCodeAsync(code);
            if (book is null) return false;
            update(book);
            var res = await _ctx.Books.ReplaceOneAsync(b => b.Id == book.Id, book);
            return res.ModifiedCount == 1;
        }

        public async Task<bool> DeleteByCodeAsync(string code)
        {
            var res = await _ctx.Books.DeleteOneAsync(b => b.Code == code);
            return res.DeletedCount == 1;
        }

        public async Task<bool> AdjustStockAndSoldAsync(string code, int qty)
        {
            // Decrease stock, increase sold when purchasing
            var update = Builders<Book>.Update
                .Inc(b => b.InStock, -qty)
                .Inc(b => b.Sold, qty);

            var res = await _ctx.Books.UpdateOneAsync(
                b => b.Code == code && b.InStock >= qty,
                update);

            return res.ModifiedCount == 1;
        }

        // NEW: increase stock (import inventory)
        public async Task<bool> IncreaseStockAsync(string code, int qty)
        {
            if (qty <= 0) return false;
            var update = Builders<Book>.Update.Inc(b => b.InStock, qty);
            var res = await _ctx.Books.UpdateOneAsync(b => b.Code == code, update);
            return res.ModifiedCount == 1;
        }

        // NEW: get all books (no paging/filter)
        public Task<List<Book>> GetAllAsync() =>
            _ctx.Books.Find(Builders<Book>.Filter.Empty).ToListAsync();

        // NEW: top sold books
        public Task<List<Book>> GetTopSoldAsync(int limit) =>
            _ctx.Books.Find(Builders<Book>.Filter.Empty)
                .Sort(Builders<Book>.Sort.Descending(b => b.Sold))
                .Limit(limit)
                .ToListAsync();

        public async Task<List<(string? CategoryCode, string? CategoryName, int TotalSold)>> GetTopCategoriesAsync(int limit)
        {
            var pipeline = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "$group", new BsonDocument
                        {
                            { "_id", new BsonDocument
                                {
                                    { "CategoryCode", "$loai.maloai" },
                                    { "CategoryName", "$loai.tenloai" }
                                }
                            },
                            { "TotalSold", new BsonDocument { { "$sum", "$soluongdaban" } } }
                        }
                    }
                },
                new BsonDocument
                {
                    { "$sort", new BsonDocument { { "TotalSold", -1 } } }
                },
                new BsonDocument
                {
                    { "$limit", limit }
                }
            };

            var result = await _ctx.Books.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return result.Select(doc => (
                CategoryCode: doc["_id"]["CategoryCode"].AsString,
                CategoryName: doc["_id"]["CategoryName"].AsString,
                TotalSold: doc["TotalSold"].AsInt32
            )).ToList();
        }

        // NEW: Get all distinct book categories
        public async Task<List<BookCategory>> GetAllCategoriesAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("loai", new BsonDocument("$ne", BsonNull.Value))),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "maloai", "$loai.maloai" },
                            { "tenloai", "$loai.tenloai" }
                        }
                    }
                }),
                new BsonDocument("$sort", new BsonDocument("_id.tenloai", 1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "maloai", "$_id.maloai" },
                    { "tenloai", "$_id.tenloai" }
                })
            };

            var result = await _ctx.Books.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            return result.Select(doc => new BookCategory
            {
                Code = doc.Contains("maloai") ? doc["maloai"].AsString : null,
                Name = doc.Contains("tenloai") ? doc["tenloai"].AsString : null
            }).ToList();
        }

        // NEW: Get all distinct publishers
        public async Task<List<Publisher>> GetAllPublishersAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("nxb", new BsonDocument("$ne", BsonNull.Value))),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "manxb", "$nxb.manxb" },
                            { "tennxb", "$nxb.tennxb" },
                            { "diachi", "$nxb.diachi" }
                        }
                    }
                }),
                new BsonDocument("$sort", new BsonDocument("_id.tennxb", 1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "manxb", "$_id.manxb" },
                    { "tennxb", "$_id.tennxb" },
                    { "diachi", "$_id.diachi" }
                })
            };

            var result = await _ctx.Books.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            return result.Select(doc => new Publisher
            {
                Code = doc.Contains("manxb") ? doc["manxb"].AsString : null,
                Name = doc.Contains("tennxb") ? doc["tennxb"].AsString : null,
                Address = doc.Contains("diachi") ? doc["diachi"].AsString : null
            }).ToList();
        }

        // NEW: Generate unique book code (format: SPDDMMYYhhss)
        public string GenerateBookCode()
        {
            var now = DateTime.Now;
            return $"SP{now:ddMMyyHHmmss}"; // SP291025143025 (29/10/25 14:30:25)
        }
    }
}
