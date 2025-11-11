using System.Net.Http.Json;
using System.Text.Json;
using API_NoSQL.Dtos;
using API_NoSQL.Models;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace API_NoSQL.Services
{
    public class FirebaseEmailAuthService
    {
        private readonly CustomerService _customers;
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private static bool _initialized;

        public FirebaseEmailAuthService(CustomerService customers, IHttpClientFactory httpClientFactory)
        {
            _customers = customers;
            _http = httpClientFactory.CreateClient(nameof(FirebaseEmailAuthService));
            _apiKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY")
                ?? throw new InvalidOperationException("FIREBASE_API_KEY is not set.");
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

            if (FirebaseApp.DefaultInstance != null)
            {
                _initialized = true;
                return;
            }

            var credPath = Environment.GetEnvironmentVariable("FIREBASE_CRED_PATH");
            var resolved = ResolvePath(credPath);
            if (resolved is not null)
            {
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(resolved) });
                _initialized = true;
                return;
            }

            var b64 = Environment.GetEnvironmentVariable("FIREBASE_CRED_JSON_BASE64");
            if (!string.IsNullOrWhiteSpace(b64))
            {
                var bytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(bytes);
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromStream(ms) });
                _initialized = true;
                return;
            }

            var raw = Environment.GetEnvironmentVariable("FIREBASE_CRED_JSON");
            if (!string.IsNullOrWhiteSpace(raw))
            {
                using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(raw));
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromStream(ms) });
                _initialized = true;
                return;
            }
        }

        private string Endpoint(string path) => $"https://identitytoolkit.googleapis.com/v1/{path}?key={_apiKey}";

        private static JsonSerializerOptions JsonOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<(bool Ok, string? Error, Customer? Customer)> RegisterAsync(EmailRegisterDto dto)
        {
            try
            {
                var existing = await _customers.GetByUsernameAsync(dto.Email);
                if (existing is not null)
                    return (false, "Email already registered", null);

                if (FirebaseApp.DefaultInstance == null)
                {
                    EnsureFirebase();
                }

                var signUpPayload = new
                {
                    email = dto.Email,
                    password = dto.Password,
                    returnSecureToken = true
                };
                
                var signUpResp = await _http.PostAsJsonAsync(Endpoint("accounts:signUp"), signUpPayload, JsonOptions);
                var signUpJson = await signUpResp.Content.ReadFromJsonAsync<JsonElement>();
                
                if (!signUpResp.IsSuccessStatusCode)
                {
                    var err = signUpJson.TryGetProperty("error", out var e) 
                        && e.TryGetProperty("message", out var msg)
                        ? msg.GetString() 
                        : "Registration failed";
                    return (false, err, null);
                }

                var idToken = signUpJson.GetProperty("idToken").GetString();
                if (!string.IsNullOrWhiteSpace(idToken))
                {
                    await _http.PostAsJsonAsync(Endpoint("accounts:sendOobCode"), new
                    {
                        requestType = "VERIFY_EMAIL",
                        idToken
                    }, JsonOptions);
                }

                static string NewCustomerCode() => $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                var customer = new Customer
                {
                    Code = NewCustomerCode(),
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Address = dto.Address,
                    Avatar = null,
                    Account = new Account
                    {
                        Username = dto.Email,
                        Role = "khachhang",
                        Status = "ChuaXacMinh"
                    },
                    Orders = new List<Order>()
                };

                await _customers.CreateAsync(customer, Guid.NewGuid().ToString("N"));
                customer.Account.PasswordHash = string.Empty;
                
                return (true, null, customer);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Ok, string? Error, Customer? Customer)> LoginAsync(EmailLoginDto dto)
        {
            try
            {
                var signInPayload = new
                {
                    email = dto.Email,
                    password = dto.Password,
                    returnSecureToken = true
                };

                var signInResp = await _http.PostAsJsonAsync(Endpoint("accounts:signInWithPassword"), signInPayload, JsonOptions);
                var signInJson = await signInResp.Content.ReadFromJsonAsync<JsonElement>();
                if (!signInResp.IsSuccessStatusCode)
                {
                    var err = signInJson.TryGetProperty("error", out var e) ? e.ToString() : "signIn failed";
                    return (false, err, null);
                }

                var idToken = signInJson.GetProperty("idToken").GetString();
                if (string.IsNullOrWhiteSpace(idToken))
                    return (false, "No idToken returned", null);

                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                decoded.Claims.TryGetValue("email", out var emailObj);
                decoded.Claims.TryGetValue("email_verified", out var verifiedObj);

                var email = emailObj?.ToString();
                var emailVerified = verifiedObj is bool b && b;

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email not present in token", null);
                if (!emailVerified)
                    return (false, "Email is not verified", null);

                var user = await _customers.GetByUsernameAsync(email);
                if (user is null)
                {
                    static string NewCustomerCode() => $"KH{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                    user = new Customer
                    {
                        Code = NewCustomerCode(),
                        FullName = email.Split('@')[0],
                        Phone = string.Empty,
                        Email = email,
                        Address = string.Empty,
                        Avatar = null,
                        Account = new Account
                        {
                            Username = email,
                            Role = "khachhang",
                            Status = "DaXacMinh"
                        },
                        Orders = new List<Order>()
                    };
                    await _customers.CreateAsync(user, dto.Password);
                }
                else
                {
                    if (!_customers.VerifyPassword(user, dto.Password))
                    {
                        var newHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                        await _customers.UpdateAsync(user.Code, c =>
                        {
                            c.Account.PasswordHash = newHash;
                        });
                    }

                    if (user.Account.Status == "ChuaXacMinh" && emailVerified)
                    {
                        await _customers.UpdateAsync(user.Code, c => c.Account.Status = "DaXacMinh");
                        user.Account.Status = "DaXacMinh";
                    }
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

        public async Task<bool> SendVerificationEmailAsync(string idToken)
        {
            var resp = await _http.PostAsJsonAsync(Endpoint("accounts:sendOobCode"), new
            {
                requestType = "VERIFY_EMAIL",
                idToken
            }, JsonOptions);
            return resp.IsSuccessStatusCode;

        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            var resp = await _http.PostAsJsonAsync(Endpoint("accounts:sendOobCode"), new
            {
                requestType = "PASSWORD_RESET",
                email
            }, JsonOptions);
            return resp.IsSuccessStatusCode;
        }

        public async Task<(bool Ok, string? Error)> StartChangeEmailAsync(string customerCode, string idToken, string newEmail)
        {
            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                decoded.Claims.TryGetValue("email", out var currentEmailObj);
                var currentEmail = currentEmailObj?.ToString();

                if (string.IsNullOrWhiteSpace(currentEmail))
                    return (false, "Email not found in token");

                var customer = await _customers.GetByCodeAsync(customerCode);
                if (customer is null)
                    return (false, "Customer not found");

                var existing = await _customers.GetByUsernameAsync(newEmail);
                if (existing is not null && existing.Code != customerCode)
                    return (false, "Email already in use");

                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(currentEmail);
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    Email = newEmail,
                    EmailVerified = false
                });

                var updated = await _customers.UpdateAsync(customerCode, c =>
                {
                    c.Email = newEmail;
                    c.Account.Username = newEmail;
                    c.Account.Status = "ChuaXacMinh";
                });

                if (!updated)
                {
                    await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                    {
                        Uid = userRecord.Uid,
                        Email = currentEmail,
                        EmailVerified = true
                    });
                    return (false, "Failed to update database");
                }

                await _http.PostAsJsonAsync(Endpoint("accounts:sendOobCode"), new
                {
                    requestType = "VERIFY_EMAIL",
                    idToken
                }, JsonOptions);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Ok, string? Error)> ChangePasswordAsync(ChangePasswordDto dto)
        {
            try
            {
                var signInPayload = new
                {
                    email = dto.Email,
                    password = dto.CurrentPassword,
                    returnSecureToken = true
                };

                var signInResp = await _http.PostAsJsonAsync(Endpoint("accounts:signInWithPassword"), signInPayload, JsonOptions);
                if (!signInResp.IsSuccessStatusCode)
                {
                    return (false, "Current password is incorrect");
                }

                var signInJson = await signInResp.Content.ReadFromJsonAsync<JsonElement>();
                var idToken = signInJson.GetProperty("idToken").GetString();
                if (string.IsNullOrWhiteSpace(idToken))
                    return (false, "Failed to verify current password");

                var changePasswordPayload = new
                {
                    idToken = idToken,
                    password = dto.NewPassword,
                    returnSecureToken = true
                };

                var changeResp = await _http.PostAsJsonAsync(Endpoint("accounts:update"), changePasswordPayload, JsonOptions);
                if (!changeResp.IsSuccessStatusCode)
                {
                    var changeJson = await changeResp.Content.ReadFromJsonAsync<JsonElement>();
                    var err = changeJson.TryGetProperty("error", out var e) 
                        && e.TryGetProperty("message", out var msg)
                        ? msg.GetString() 
                        : "Failed to change password";
                    return (false, err);
                }

                var user = await _customers.GetByUsernameAsync(dto.Email);
                if (user != null)
                {
                    var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    await _customers.UpdateAsync(user.Code, c =>
                    {
                        c.Account.PasswordHash = newHash;
                    });
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}