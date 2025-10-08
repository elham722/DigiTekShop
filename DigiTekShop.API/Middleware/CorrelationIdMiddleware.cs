using Serilog.Context;

namespace DigiTekShop.API.Middleware;

/// <summary>
/// Middleware to add a Correlation ID to each request
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _headerName;

    public CorrelationIdMiddleware(RequestDelegate next, string headerName = "X-Request-ID")
    {
        _next = next;
        _headerName = headerName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or create correlation ID
        var correlationId = context.Request.Headers[_headerName].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_headerName))
            {
                context.Response.Headers[_headerName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // Add to Serilog LogContext
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder, string headerName = "X-Request-ID")
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>(headerName);
    }
}
