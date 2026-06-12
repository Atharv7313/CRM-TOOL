using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.CL.Services;
using Shared.Filters;
using System.Text;

namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication + authorization.
    /// Reads Jwt:Secret and Jwt:ExpiryMinutes from config.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ClockSkew                = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Registers a CORS policy named "Angular".
    /// Origins are read from Cors:Origins in config; falls back to localhost:53719.
    /// </summary>
    public static IServiceCollection AddAngularCors(
        this IServiceCollection services,
        IConfiguration config)
    {
        var origins = config.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:53719", "http://127.0.0.1:53719"];

        services.AddCors(opt =>
            opt.AddPolicy("Angular", p =>
                p.WithOrigins(origins)
                 .AllowAnyHeader()
                 .AllowAnyMethod()));

        return services;
    }

 

    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        string title       = "API",
        string description = "")
    {
        // OpenApi is registered directly in each service's Program.cs
        return services;
    }

    // JWT is configured directly in each service's Program.cs
    // AddJwtAuthentication is intentionally removed to avoid version conflicts
    /// <summary>
    /// Registers controllers with the shared GlobalExceptionFilter applied globally.
    /// </summary>
    public static IServiceCollection AddSharedControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            options.Filters.Add<GlobalExceptionFilter>());

        return services;
    }

    /// <summary>
    /// Registers the central IAuditClient pointing at AuditLogService.API.
    /// Base URL is read from AuditService:BaseUrl (default: http://localhost:5298/).
    /// </summary>
    public static IServiceCollection AddAuditClient(
        this IServiceCollection services,
        IConfiguration config)
    {
        var baseUrl = config["AuditService:BaseUrl"] ?? "http://localhost:5298/";

        services.AddHttpClient<IAuditClient, HttpAuditClient>(c =>
        {
            c.BaseAddress = new Uri(baseUrl);
            c.Timeout     = TimeSpan.FromSeconds(3);
        });

        return services;
    }
}
