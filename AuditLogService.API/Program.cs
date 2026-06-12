using AuditLogService.API.Data;
using AuditLogService.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database (LifeSci360_Audit) ───────────────────────────
builder.Services.AddDbContext<AuditLogDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("AuditDb"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// ── Repository ────────────────────────────────────────────
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ── Controllers ───────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "LifeSci360 — AuditLogService", Version = "v1" }));

// ── CORS — allow Angular + all internal services ──────────
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:53719", "http://localhost:4200"];

builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p =>
        p.WithOrigins(origins)
         .AllowAnyHeader()
         .AllowAnyMethod()));

// ── Auth (no JWT required — endpoints are AllowAnonymous) ─
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Bootstrap AuditLogs table: create if missing, ALTER in any missing columns ──
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>();
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw(AuditLogsBootstrap.Sql);
        Console.WriteLine("[AuditLogService] Database ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AuditLogService] DB init warning: {ex.Message}");
        Console.WriteLine("[AuditLogService] Run create_audit_db.sql in SSMS to create DB manually.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuditLogService v1"));

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
