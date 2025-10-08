namespace DigiTekShop.API.Middleware;

/// <summary>
/// Middleware for API Key authentication (optional - for service-to-service calls)
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // Skip for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api-docs"))
        {
            await _next(context);
            return;
        }

        // Check if API Key validation is enabled
        var apiKeyEnabled = configuration.GetValue<bool>("ApiKey:Enabled", false);
        if (!apiKeyEnabled)
        {
            await _next(context);
            return;
        }

        // Check for API Key header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API Key missing in request from {IP}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "API Key is missing",
                code = "API_KEY_MISSING"
            });
            return;
        }

        // Validate API Key
        var validApiKeys = configuration.GetSection("ApiKey:ValidKeys").Get<string[]>() ?? Array.Empty<string>();
        
        if (!validApiKeys.Contains(extractedApiKey.ToString()))
        {
            _logger.LogWarning("Invalid API Key attempted from {IP}: {Key}", 
                context.Connection.RemoteIpAddress, 
                extractedApiKey.ToString().Substring(0, Math.Min(8, extractedApiKey.ToString().Length)));
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Invalid API Key",
                code = "API_KEY_INVALID"
            });
            return;
        }

        _logger.LogDebug("Valid API Key authenticated from {IP}", context.Connection.RemoteIpAddress);
        await _next(context);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKey(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}

