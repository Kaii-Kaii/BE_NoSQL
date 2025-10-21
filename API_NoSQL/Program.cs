using API_NoSQL.Services;
using API_NoSQL.Settings;

var builder = WebApplication.CreateBuilder(args);

// Bind Mongo settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));

// Bind Cloudinary settings
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));

// Add services
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StatsService>(); // NEW
builder.Services.AddScoped<CloudinaryService>(); // NEW: uploads

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
