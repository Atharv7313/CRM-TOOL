using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Repository;
using PatientService.Services;
using Shared.Extensions;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<ServicesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ServicesDb")));

// ── Repositories ──────────────────────────────────────────
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();

// ── Audit Client ──────────────────────────────────────────
builder.Services.AddAuditClient(builder.Configuration);

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<IPatientService, PatientService.Services.PatientService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IVisitService, VisitService>();

// ── Controllers + Swagger ─────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title = "LifeSci360 — PatientService API",
        Version = "v1",
        Description = "Patient · Enrollment · Visits"
    }));

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PatientService v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();
