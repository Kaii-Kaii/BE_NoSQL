using API_NoSQL.Models;
using API_NoSQL.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoDbContext(IOptions<MongoDbSettings> options, ILogger<MongoDbContext> logger)
        {
            _settings = options.Value;
            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);

            logger.LogInformation(
                "MongoDB connected. DB={Database}, BooksCollection={Books}, CustomersCollection={Customers}",
                _settings.DatabaseName, _settings.BooksCollection, _settings.CustomersCollection);
        }

        public IMongoCollection<Book> Books =>
            _database.GetCollection<Book>(_settings.BooksCollection);

        public IMongoCollection<Customer> Customers =>
            _database.GetCollection<Customer>(_settings.CustomersCollection);
    }
}