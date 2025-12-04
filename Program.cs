using DripCube.Data;
using Microsoft.EntityFrameworkCore;
using DripCube.Helpers;
using DripCube.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== STARTING DRIPCUBE ===");

// Hardcoded database connection
var connectionString = "Host=dpg-d4g6db8dl3ps73da19pg-a.oregon-postgres.render.com;Database=abcn;Username=user;Password=NBWohmR0QCiyPLqFd2uGR2HWMNvm9GnA;SslMode=Require;Trust Server Certificate=true;";

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

// Hardcoded Cloudinary settings
builder.Services.Configure<CloudinarySettings>(options =>
{
    options.CloudName = "dd8lrzw0s";
    options.ApiKey = "839158591392738";
    options.ApiSecret = "EnC6RHDyIYorTryGnLUTrvSRnbo";
});

builder.Services.AddScoped<PhotoService>();

// Hardcoded Stripe API key
StripeConfiguration.ApiKey = "sk_test_51SXRvaRq3TM6Cq5tdg8kEErnMzCdzZ69B0YyCTX3FAU9UDRFWzd4HE1GnKGFSpqhbkO79iy89LKABUX1dixt1Cm700ksXoe0YG";

var app = builder.Build();

// Database initialization
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✓ Database connected");

            if (!context.Employees.Any())
            {
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
                context.SaveChanges();
                Console.WriteLine("✓ Admin created: admin/admin123");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ DB Error: {ex.Message}");
}

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

Console.WriteLine("=== SERVER READY ===");
app.Run();