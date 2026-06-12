using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shared.Filters;

public class JwtAuthFilter : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _config;

    public JwtAuthFilter(IConfiguration config)
    {
        _config = config;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        bool isAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em is AllowAnonymousAttribute);

        if (isAnonymous) return Task.CompletedTask;

        string? authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new ObjectResult(new { success = false, message = "Unauthorized. Missing or invalid token." })
            {
                StatusCode = 401
            };
            return Task.CompletedTask;
        }

        string token = authHeader["Bearer ".Length..].Trim();

        try
        {
            string secret = _config["Jwt:Secret"]!;
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ClockSkew                = TimeSpan.Zero
            }, out SecurityToken validated);

            var jwt = (JwtSecurityToken)validated;
            context.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(jwt.Claims, "jwt"));
        }
        catch
        {
            context.Result = new ObjectResult(new { success = false, message = "Unauthorized. Token expired or invalid." })
            {
                StatusCode = 401
            };
        }

        return Task.CompletedTask;
    }
}
