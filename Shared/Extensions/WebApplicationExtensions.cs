using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Shared.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseSwaggerInDevelopment(
        this WebApplication app,
        string title = "API v1")
    {
        // MapOpenApi is called directly in each service's Program.cs
        return app;
    }

    public static WebApplication UseSharedMiddleware(this WebApplication app)
    {
        app.UseCors("Angular");
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
