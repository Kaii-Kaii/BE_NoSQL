using API_NoSQL.Dtos;
using API_NoSQL.Models;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace API_NoSQL.Services
{
    public class GoogleAuthService
    {
        private readonly CustomerService _customers;
        private static bool _initialized;

        public GoogleAuthService(CustomerService customers)
        {
            _customers = customers;
            EnsureFirebase();
        }

        private static string? ResolvePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (Path.IsPathRooted(path) && File.Exists(path)) return path;

            var bases = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (var b in bases)
            {
                var p = Path.Combine(b, path);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static void EnsureFirebase()
        {
            if (_initialized) return;

            // 1) Prefer PATH (absolute or relative to app root)
            var credPath = Environment.GetEnvironmentVariable("FIREBASE_CRED_PATH");
            var resolved = ResolvePath(credPath);
            if (resolved is not null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(resolved)
                });
                _initialized = true;
                return;
            }

            // 2) Or BASE64 JSON
            var b64 = Environment.GetEnvironmentVariable("FIREBASE_CRED_JSON_BASE64");
            if (!string.IsNullOrWhiteSpace(b64))
            {
                var bytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(bytes);
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromStream(ms)
                });
                _initialized = true;
                return;
            }

            // 3) Or RAW JSON (single line with \n in private_key)
            var raw = Environment.GetEnvironmentVariable("FIREBASE_CRED_JSON");
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(raw));
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromStream(ms)
                });
                _initialized = true;
                return;
            }
        }

        public async Task<(bool Ok, string? Error, Customer? Customer)> LoginWithGoogleAsync(string idToken)
        {
            try
            {
                EnsureFirebase();
                if (!_initialized) return (false, "Firebase not configured", null);

                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                decoded.Claims.TryGetValue("email", out var emailObj);
                decoded.Claims.TryGetValue("name", out var nameObj);
                decoded.Claims.TryGetValue("picture", out var pictureObj);

                var email = emailObj?.ToString();
                var name = nameObj?.ToString() ?? "User";
                var avatar = pictureObj?.ToString();

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email not present in token", null);

                var user = await _customers.GetByUsernameAsync(email);
                if (user is null)
                {
                    static string NewCustomerCode() => $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                    user = new Customer
                    {
                        Code = NewCustomerCode(),
                        FullName = name,
                        Phone = string.Empty,
                        Email = email,
                        Address = string.Empty,
                        Avatar = avatar,
                        Account = new Account
                        {
                            Username = email,
                            Role = "khachhang"
                        },
                        Orders = new List<Order>()
                    };

                    await _customers.CreateAsync(user, Guid.NewGuid().ToString("N"));
                }

                user.Account.PasswordHash = string.Empty;
                return (true, null, user);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, $"Invalid token: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }
    }
}
