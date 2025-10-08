namespace DigiTekShop.API.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ✅ X-Content-Type-Options: Prevent MIME-sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // ✅ X-Frame-Options: Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // ✅ X-XSS-Protection: Enable XSS filter (legacy browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // ✅ Referrer-Policy: Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // ✅ Content-Security-Policy: Prevent XSS and other injection attacks
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";

        // ✅ Permissions-Policy: Control browser features
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // ✅ Strict-Transport-Security (HSTS): Force HTTPS
        // Note: Only add in production/HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // ✅ Remove server identification headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

