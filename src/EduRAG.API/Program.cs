using BCrypt.Net;
using EduRAG.API.Middleware;
using EduRAG.Domain.Entities;
using EduRAG.Domain.Enums;
using EduRAG.Infrastructure;
using EduRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// JWT Auth
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// CORS — specific origins only
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

// Rate limiting on /api/chat endpoints
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("ChatLimit", opt =>
    {
        opt.PermitLimit          = 20;
        opt.Window               = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit           = 0;
    });
    o.RejectionStatusCode = 429;
});

// ── App Pipeline ───────────────────────────────────────────────────────────
var app = builder.Build();

// Seed default admin on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.AppUsers.AnyAsync(u => u.Role == UserRole.Admin))
    {
        db.AppUsers.Add(new AppUser
        {
            Id           = Guid.NewGuid(),
            FullName     = "Admin",
            Email        = "admin@edurag.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11),
            Role         = UserRole.Admin,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
