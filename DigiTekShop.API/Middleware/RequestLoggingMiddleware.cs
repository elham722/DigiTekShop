using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace DigiTekShop.API.Middleware;

public sealed class RequestLoggingMiddleware
{
    private static readonly HashSet<string> _skipPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/swagger", "/api-docs"
    };

    private static readonly HashSet<string> _textContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json", "application/problem+json", "text/plain", "text/html", "text/css", "application/xml"
    };

    private const int MaxBodyBytes = 64 * 1024; 

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _isDevelopment;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        _isDevelopment = (configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production")
                         .Equals("Development", StringComparison.OrdinalIgnoreCase);

        _logRequestBody = configuration.GetValue("Logging:LogRequestBody", false);
        _logResponseBody = configuration.GetValue("Logging:LogResponseBody", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ct = context.RequestAborted;

        var path = context.Request.Path.Value ?? string.Empty;
        var skipThisPath = _skipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        var sw = Stopwatch.StartNew();

        
        var correlationId = TryGetCorrelationId(context) ?? context.TraceIdentifier;

        
        if (_isDevelopment && _logRequestBody && !skipThisPath)
        {
            await LogRequestAsync(context, correlationId, ct);
        }
        else
        {
            _logger.LogDebug("HTTP {Method} {Path}{Query} (cid={CorrelationId})",
                context.Request.Method, context.Request.Path, context.Request.QueryString, correlationId);
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            if (_isDevelopment && _logResponseBody && !skipThisPath)
            {
                await LogResponseAsync(context, buffer, sw.ElapsedMilliseconds, correlationId, ct);
            }
            else
            {
                _logger.LogDebug("HTTP {Status} {Method} {Path} in {Elapsed}ms (cid={CorrelationId})",
                    context.Response.StatusCode, context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds, correlationId);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, ct);
            context.Response.Body = originalBody;
        }
    }

    private static string? TryGetCorrelationId(HttpContext ctx)
    {
        
        if (ctx.Items.TryGetValue("X-Correlation-ID", out var v) && v is string s && !string.IsNullOrWhiteSpace(s))
            return s;

        
        if (ctx.Request.Headers.TryGetValue("X-Request-ID", out var hdr) && !StringValues.IsNullOrEmpty(hdr))
            return hdr.ToString();

        return null;
    }

    private async Task LogRequestAsync(HttpContext ctx, string correlationId, CancellationToken ct)
    {
        ctx.Request.EnableBuffering();

        var headers = RedactHeaders(ctx.Request.Headers);

        string bodyLog = "(no body)";
        if (ctx.Request.ContentLength is > 0 && IsTextLike(ctx.Request.ContentType))
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            ctx.Request.Body.Position = 0;

            var limited = await ReadLimitedAsync(reader, MaxBodyBytes, ct);
            ctx.Request.Body.Position = 0;

            bodyLog = RedactBody(limited, ctx.Request.ContentType);
        }

        _logger.LogDebug(
            "HTTP Request {Method} {Path}{Query} (cid={CorrelationId})\nHeaders: {Headers}\nBody: {Body}",
            ctx.Request.Method, ctx.Request.Path, ctx.Request.QueryString, correlationId, headers, bodyLog);
    }

    private async Task LogResponseAsync(HttpContext ctx, MemoryStream responseBody, long elapsedMs, string correlationId, CancellationToken ct)
    {
        responseBody.Position = 0;

        var headers = RedactHeaders(ctx.Response.Headers);

        string bodyLog = "(no body)";
        if (responseBody.Length > 0 && IsTextLike(ctx.Response.ContentType))
        {
            using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
            var limited = await ReadLimitedAsync(reader, MaxBodyBytes, ct);
            responseBody.Position = 0;

            bodyLog = RedactBody(limited, ctx.Response.ContentType);
        }

        _logger.LogDebug(
            "HTTP Response {Status} in {Elapsed}ms (cid={CorrelationId})\nHeaders: {Headers}\nBody: {Body}",
            ctx.Response.StatusCode, elapsedMs, correlationId, headers, bodyLog);
    }

    private static bool IsTextLike(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var type = contentType.Split(';')[0].Trim();
        return _textContentTypes.Contains(type) || type.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, int maxBytes, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buf = new char[4096];
        var readBytes = 0;

        while (!reader.EndOfStream && readBytes < maxBytes)
        {
            var toRead = Math.Min(buf.Length, (maxBytes - readBytes) / sizeof(char) + 1);
            var n = await reader.ReadAsync(buf.AsMemory(0, toRead), ct);
            if (n == 0) break;
            sb.Append(buf, 0, n);
            readBytes += Encoding.UTF8.GetByteCount(buf.AsSpan(0, n));
        }

        if (!reader.EndOfStream) sb.Append("…(truncated)");
        return sb.ToString();
    }

    private static IDictionary<string, string> RedactHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers)
        {
            var name = h.Key;
            if (name.Equals(HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase)
                || name.Equals(HeaderNames.Cookie, StringComparison.OrdinalIgnoreCase)
                || name.Equals(HeaderNames.SetCookie, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = "***redacted***";
            }
            else
            {
                result[name] = h.Value.ToString();
            }
        }
        return result;
    }

    private static string RedactBody(string body, string? contentType)
    {
        
        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                "(\"(?:password|token|secret|refreshToken)\"\\s*:\\s*\")([^\"]+)(\")",
                "$1***redacted***$3",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return body;
    }
}


