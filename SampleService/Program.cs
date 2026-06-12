using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SampleService.Data;
using SampleService.Extensions;
using SampleService.Repositories;
using SampleService.Services;
using Shared.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddAngularCors(builder.Configuration);

// Database
builder.Services.AddDbContext<ServicesDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ServicesDb"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(12), errorNumbersToAdd: null)));

// JWT — configured directly to avoid any Shared.Extensions issues
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
                // Manually extract token to avoid any extraction bugs
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    ctx.Token = auth.Substring(7).Trim();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Audit Client
builder.Services.AddAuditClient(builder.Configuration);

// Services
builder.Services.AddScoped<ISampleRepository, SampleRepository>();
builder.Services.AddScoped<ILabResultRepository, LabResultRepository>();
builder.Services.AddScoped<ISampleService, SampleService.Services.SampleService>();
builder.Services.AddScoped<ILabResultService, LabResultService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSharedControllers();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerInDevelopment();
app.UseSharedMiddleware();
app.MapControllers();
app.Run();
