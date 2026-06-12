using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Shared.Filters;

public class RoleAuthorizeFilter : IAsyncAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RoleAuthorizeFilter(string[] roles)
    {
        _allowedRoles = roles;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        bool isAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em is AllowAnonymousAttribute);

        if (isAnonymous) return Task.CompletedTask;

        ClaimsPrincipal user = context.HttpContext.User;

        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ObjectResult(new { success = false, message = "Unauthorized." })
            {
                StatusCode = 401
            };
            return Task.CompletedTask;
        }

        if (_allowedRoles.Length == 0) return Task.CompletedTask;

        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value);

        bool hasAccess = _allowedRoles.Any(r => userRoles.Contains(r));

        if (!hasAccess)
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                message = "Forbidden. You do not have the required permissions."
            })
            {
                StatusCode = 403
            };
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Use on controllers/actions: [RoleAuthorize(RoleNames.Admin, RoleNames.DataManager)]
/// </summary>
public class RoleAuthorizeAttribute : TypeFilterAttribute
{
    public RoleAuthorizeAttribute(params string[] roles)
        : base(typeof(RoleAuthorizeFilter))
    {
        Arguments = new object[] { roles };
    }
}
