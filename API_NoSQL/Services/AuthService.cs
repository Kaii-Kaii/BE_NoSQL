using API_NoSQL.Dtos;
using API_NoSQL.Models;

namespace API_NoSQL.Services
{
    public class AuthService
    {
        private readonly CustomerService _customers;

        public AuthService(CustomerService customers) => _customers = customers;

        public async Task<(bool Ok, string? Error, Customer? Customer)> LoginAsync(LoginDto dto)
        {
            var user = await _customers.GetByUsernameAsync(dto.Username);
            if (user is null) return (false, "User not found", null);
            if (!_customers.VerifyPassword(user, dto.Password))
                return (false, "Invalid credentials", null);

            user.Account.PasswordHash = string.Empty;
            return (true, null, user);
        }
    }
}