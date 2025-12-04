using DripCube.Data;
using Microsoft.EntityFrameworkCore;
using DripCube.Helpers;
using DripCube.Services;
using Stripe;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Use environment variable for database connection (set in Render dashboard)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=dpg-d4g6db8dl3ps73da19pg-a.oregon-postgres.render.com;Database=abcn;Username=user;Password=NBWohmR0QCiyPLqFd2uGR2HWMNvm9GnA;SslMode=Require;Trust Server Certificate=true;";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHostedService<DripCube.Services.ChatCleanupService>();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<PhotoService>();

// Use configuration for Stripe API key (can be overridden by environment variable)
StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
    ?? builder.Configuration["Stripe:SecretKey"]
    ?? "sk_test_51SXRvaRq3TM6Cq5tdg8kEErnMzCdzZ69B0YyCTX3FAU9UDRFWzd4HE1GnKGFSpqhbkO79iy89LKABUX1dixt1Cm700ksXoe0YG";

var app = builder.Build();

Console.WriteLine("=== APPLICATION STARTING ===");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"Connection String (first 50 chars): {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

// Database initialization with error handling
try
{
    using (var scope = app.Services.CreateScope())
    {
        Console.WriteLine("Creating database scope...");
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("Testing database connection...");
        await context.Database.CanConnectAsync();
        Console.WriteLine("Database connection successful!");

        if (!context.Employees.Any())
        {
            Console.WriteLine("Creating admin user...");
            var admin = new DripCube.Entities.Employee
            {
                Role = DripCube.Entities.EmployeeRole.Admin,
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                PersonalId = "ADMIN001",
                ChatId = "ADMINCHAT",
                IsActive = true,
                FirstName = "Big",
                LastName = "Boss"
            };
            context.Employees.Add(admin);
            await context.SaveChangesAsync();
            Console.WriteLine("--- ADMIN CREATED: Login: admin / Pass: admin123 ---");
        }
        else
        {
            Console.WriteLine("Admin user already exists, skipping creation.");
        }
    }
    Console.WriteLine("Database initialization completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"!!! DATABASE INITIALIZATION ERROR: {ex.Message}");
    Console.WriteLine($"!!! Stack Trace: {ex.StackTrace}");
    Console.WriteLine("!!! Application will continue but database may not be initialized!");
}

Console.WriteLine("=== STARTING WEB SERVER ===");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"=== SERVER READY - Listening on {Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "default URLs"} ===");

app.Run();