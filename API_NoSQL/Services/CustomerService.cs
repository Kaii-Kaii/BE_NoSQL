using API_NoSQL.Models;
using BCrypt.Net;
using MongoDB.Driver;

namespace API_NoSQL.Services
{
    public class CustomerService
    {
        private readonly MongoDbContext _ctx;

        public CustomerService(MongoDbContext ctx) => _ctx = ctx;

        public Task<Customer?> GetByCodeAsync(string code) =>
            _ctx.Customers.Find(c => c.Code == code).FirstOrDefaultAsync();

        public Task<Customer?> GetByUsernameAsync(string username) =>
            _ctx.Customers.Find(c => c.Account.Username == username).FirstOrDefaultAsync();

        public async Task CreateAsync(Customer c, string rawPassword)
        {
            c.Account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword);
            await _ctx.Customers.InsertOneAsync(c);
        }

        public async Task<bool> UpdateAsync(string code, Action<Customer> update)
        {
            var c = await GetByCodeAsync(code);
            if (c is null) return false;
            update(c);
            var res = await _ctx.Customers.ReplaceOneAsync(x => x.Id == c.Id, c);
            return res.ModifiedCount == 1;
        }

        public async Task<bool> DeleteAsync(string code)
        {
            var res = await _ctx.Customers.DeleteOneAsync(c => c.Code == code);
            return res.DeletedCount == 1;
        }

        public bool VerifyPassword(Customer c, string password) =>
            BCrypt.Net.BCrypt.Verify(password, c.Account.PasswordHash);

        // NEW: đổi mật khẩu theo username
        public async Task<(bool Ok, string? Error)> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            var c = await GetByUsernameAsync(username);
            if (c is null) return (false, "User not found");
            if (!VerifyPassword(c, oldPassword)) return (false, "Invalid current password");
            if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
                return (false, "New password must be different");

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var update = Builders<Customer>.Update.Set(x => x.Account.PasswordHash, newHash);

            var res = await _ctx.Customers.UpdateOneAsync(x => x.Id == c.Id, update);
            return res.ModifiedCount == 1 ? (true, null) : (false, "Password update failed");
        }
    }
}