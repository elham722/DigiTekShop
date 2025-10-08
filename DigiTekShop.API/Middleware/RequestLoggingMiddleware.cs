using System.Diagnostics;
using System.Text;

namespace DigiTekShop.API.Middleware;

/// <summary>
/// Middleware for detailed request/response logging (development/debugging)
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Only enable in Development
        var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
        _logRequestBody = isDevelopment && configuration.GetValue<bool>("Logging:LogRequestBody", false);
        _logResponseBody = isDevelopment && configuration.GetValue<bool>("Logging:LogResponseBody", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        
        // Log Request
        if (_logRequestBody && context.Request.ContentLength > 0)
        {
            await LogRequestAsync(context.Request);
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            
            // Log Response
            if (_logResponseBody && responseBody.Length > 0)
            {
                await LogResponseAsync(context.Response, responseBody, sw.ElapsedMilliseconds);
            }

            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpRequest request)
    {
        request.EnableBuffering();
        
        var body = string.Empty;
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        _logger.LogDebug(
            "HTTP Request {Method} {Path} {QueryString}\nHeaders: {Headers}\nBody: {Body}",
            request.Method,
            request.Path,
            request.QueryString,
            request.Headers,
            body);
    }

    private async Task LogResponseAsync(HttpResponse response, MemoryStream responseBody, long elapsedMs)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);

        _logger.LogDebug(
            "HTTP Response {StatusCode} in {ElapsedMs}ms\nHeaders: {Headers}\nBody: {Body}",
            response.StatusCode,
            elapsedMs,
            response.Headers,
            body);
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}

