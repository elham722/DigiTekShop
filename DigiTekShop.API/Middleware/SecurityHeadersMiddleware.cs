using Microsoft.Net.Http.Headers;

namespace DigiTekShop.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private static readonly string[] RemoveHeaders =
        ["Server", "X-Powered-By", "X-AspNet-Version", "X-AspNetMvc-Version"];

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        foreach (var h in RemoveHeaders)
            context.Response.Headers.Remove(h);

        
        context.Response.OnStarting(() =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                            || path.StartsWith("/api-docs", StringComparison.OrdinalIgnoreCase);

            var headers = context.Response.GetTypedHeaders();

            
            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
                context.Response.Headers["X-Frame-Options"] = "DENY";

            
            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            if (!context.Response.Headers.ContainsKey("Permissions-Policy"))
                context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                
                var csp = isSwagger
                    ? "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none';"
                    : "default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; frame-ancestors 'none';";

                context.Response.Headers["Content-Security-Policy"] = csp;
            }

            
            if (context.Request.IsHttps && !context.Response.Headers.ContainsKey("Strict-Transport-Security"))
            {
                var maxAgeDays = _config.GetValue("Security:HstsDays", 365);
                context.Response.Headers["Strict-Transport-Security"] =
                    $"max-age={maxAgeDays * 24 * 60 * 60}; includeSubDomains; preload";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

