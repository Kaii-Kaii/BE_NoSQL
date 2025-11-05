using API_NoSQL.Services;
using API_NoSQL.Settings;
using DotNetEnv;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb")
);
builder.Services.AddSingleton<MongoDbContext>();

var cloudinarySettings = new CloudinarySettings
{
    CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_NAME"),
    ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_APIKEY"),
    ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_APISECRET"),
    Folder = Environment.GetEnvironmentVariable("CLOUDINARY_FOLDER") ?? "assets"
};
builder.Services.AddSingleton(cloudinarySettings);
builder.Services.AddScoped<CloudinaryService>();

// Email Settings
var emailSettings = new EmailSettings
{
    DefaultEmail = Environment.GetEnvironmentVariable("DEFAULT_EMAIL") ?? "noreply@nosql.com"
};
builder.Services.AddSingleton(emailSettings);

// SendGrid Settings
var sendGridSettings = new SendGridSettings
{
    ApiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY") ?? "",
    FromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL") ?? "noreply@kaikikail.com",
    FromName = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME") ?? "KaiiKaii Shop",
    TemplateId = Environment.GetEnvironmentVariable("SENDGRID_TEMPLATE_ID") ?? ""
};
builder.Services.AddSingleton(sendGridSettings);

// Add HttpClient for FirebaseEmailAuthService
builder.Services.AddHttpClient();

builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<FirebaseEmailAuthService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<ISendGridEmailService, SendGridEmailService>();

// Configure JSON serialization to preserve Unicode characters (Vietnamese diacritics)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

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