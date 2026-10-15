using System.Text;
using CheckAppBackend.Domain;
using CheckAppBackend.Infrastructure.Data;
using CheckAppBackend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Domain status
builder.Services.AddSingleton<IServiceStatus, ServiceStatus>();

// Password & JWT services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(connectionString) &&
    (connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
     connectionString.StartsWith("Server=", StringComparison.OrdinalIgnoreCase) ||
     connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)))
{
    if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={(userInfo.Length > 1 ? userInfo[1] : "")};SSL Mode=Require;Trust Server Certificate=true";
    }

    builder.Services.AddDbContext<EcommerceMockDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<EcommerceMockDbContext>(options =>
        options.UseInMemoryDatabase("EcommerceMockDb"));
}

// Authentication & JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "e-commerce-mock-super-secret-production-signing-key-minimum-32-chars-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ecommerce-mock";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ecommerce-mock-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// OpenAPI Document
builder.Services.AddOpenApi();

// CORS
var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:3000,http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EcommerceMockDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await EcommerceMockDbSeeder.SeedAsync(db, hasher);
}

// Map Controllers
app.MapControllers();

// Health endpoints for pipeline probe & test contract
app.MapGet("/health", (IServiceStatus status) =>
    Results.Ok(new HealthResponse(status.CurrentStatus(), ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Ok(new HealthResponse("ready", ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK);

await app.RunAsync();

public partial class Program;
