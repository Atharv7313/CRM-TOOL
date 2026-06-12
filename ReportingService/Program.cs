using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReportingService.Data;
using ReportingService.Hubs;
using ReportingService.Interfaces;
using ReportingService.Repositories;
using ReportingService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("ServicesDb"),
        sql => sql.UseCompatibilityLevel(160)));

// ── Business Logic & Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IKpiCalculationService, KpiCalculationService>();
builder.Services.AddScoped<IReportValidationService, ReportValidationService>();
builder.Services.AddScoped<IKpiReportRepository, KpiReportRepository>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(opt =>
{
    opt.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opt.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opt.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// ── Background broadcast every 30s ───────────────────────────────────────────
builder.Services.AddHostedService<DashboardBroadcastService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
// Validates the symmetric (HmacSha256) JWTs issued by AuthService using the shared
// Jwt:Secret. Issuer/Audience are not validated — AuthService does not set them.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ClockSkew                = TimeSpan.Zero
        };
        opt.Events = new()
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/reporting"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── CORS ──────────────────────────────────────────────────────────────────────
// Allow the Angular dev server. Origins come from Cors:Origins (array) in config,
// falling back to the project's standard dev ports. AllowCredentials requires
// explicit origins (no wildcard), which is why each origin is listed.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:53719", "http://127.0.0.1:53719",
        "http://localhost:4200",  "http://127.0.0.1:4200"
    };

builder.Services.AddCors(o =>
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(corsOrigins)
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ReportingHub>("/hubs/reporting");

app.Run();