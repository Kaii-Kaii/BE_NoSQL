using API_NoSQL.Services;
using API_NoSQL.Settings;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// 1️⃣ Load biến môi trường từ file .env (cho Cloudinary)
// ======================================================
Env.Load();

// ======================================================
// 2️⃣ MongoDB từ appsettings.json (vì local, không cần ẩn)
// ======================================================
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb")
);
builder.Services.AddSingleton<MongoDbContext>();

// ======================================================
// 3️⃣ Cloudinary từ .env
// ======================================================
var cloudinarySettings = new CloudinarySettings
{
    CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_NAME"),
    ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_APIKEY"),
    ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_APISECRET"),
    Folder = Environment.GetEnvironmentVariable("CLOUDINARY_FOLDER") ?? "assets"
};
builder.Services.AddSingleton(cloudinarySettings);
builder.Services.AddScoped<CloudinaryService>();

// ======================================================
// 4️⃣ Các service khác
// ======================================================
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StatsService>();

// ======================================================
// 5️⃣ Cấu hình Web API
// ======================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ======================================================
// 6️⃣ Middleware & Swagger UI
// ======================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
