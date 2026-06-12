using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Helpers;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddAngularCors(builder.Configuration);

// Database
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ServicesDb"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(12), errorNumbersToAdd: null)));

// JWT — configured directly
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.UseSecurityTokenValidators = true; // Use classic JwtSecurityTokenHandler
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false
        };
        opt.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    ctx.Token = auth.Substring(7).Trim();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Audit Client (central)
builder.Services.AddAuditClient(builder.Configuration);

// Services
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddSingleton<JwtHelper>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerWithJwt(title: "AuthService API", description: "Authentication & session management for LifeSci360");

var app = builder.Build();

// Apply pending EF migrations (creates AuditLogs table on first run)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
    db.Database.Migrate();
}

// Ensure SYSTEM_ADMIN role exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
    var systemAdminRole = db.Roles.FirstOrDefault(r => r.RoleName == "SYSTEM_ADMIN");
    if (systemAdminRole == null)
    {
        systemAdminRole = new Role { RoleName = "SYSTEM_ADMIN", IsActive = true };
        db.Roles.Add(systemAdminRole);
        db.SaveChanges();
    }
    var systemAdminUser = db.Users.FirstOrDefault(u => u.Email == "admin@lifesci360.com");
    if (systemAdminUser != null && systemAdminUser.RoleId != systemAdminRole.RoleId)
    {
        systemAdminUser.RoleId = systemAdminRole.RoleId;
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerInDevelopment("AuthService v1");
app.UseSharedMiddleware();
app.MapControllers();
app.Run();
