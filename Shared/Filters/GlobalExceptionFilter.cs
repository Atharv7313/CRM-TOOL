using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Shared.Filters;

public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception: {Path}", context.HttpContext.Request.Path);

        context.Result = new ObjectResult(new
        {
            success = false,
            message = "An unexpected error occurred. Please try again later.",
            error   = context.Exception.Message
        })
        {
            StatusCode = 500
        };

        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}
