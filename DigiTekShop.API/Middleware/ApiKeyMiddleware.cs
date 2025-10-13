using System.Security.Cryptography;
using DigiTekShop.API.Common.Http;
using DigiTekShop.API.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DigiTekShop.API.Middleware;

public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IOptionsMonitor<ApiKeyOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var opts = _options.CurrentValue;

        if (!opts.Enabled)
        {
            await _next(context);
            return;
        }

        var requiresApiKey = context.GetEndpoint()?.Metadata?.GetMetadata<RequireApiKeyAttribute>() is not null;
        if (!requiresApiKey)
        {
            await _next(context);
            return;
        }

        var headerName = string.IsNullOrWhiteSpace(opts.HeaderName) ? "X-API-Key" : opts.HeaderName;

        if (!context.Request.Headers.TryGetValue(headerName, out var provided) || string.IsNullOrWhiteSpace(provided))
        {
            _logger.LogWarning("API key missing (ip={IP})", context.Connection.RemoteIpAddress);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "API_KEY_MISSING",
                "API Key required", $"The '{headerName}' header is required.");
            return;
        }

        var ok = false;
        foreach (var k in opts.ValidKeys ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(k)) continue;
            var a = System.Text.Encoding.UTF8.GetBytes(k);
            var b = System.Text.Encoding.UTF8.GetBytes(provided.ToString());
            if (a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b))
            {
                ok = true; break;
            }
        }

        if (!ok)
        {
            _logger.LogWarning("Invalid API key (ip={IP})", context.Connection.RemoteIpAddress);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "API_KEY_INVALID",
                "Invalid API Key", "Provided API key is invalid.");
            return;
        }

        _logger.LogDebug("API key accepted (ip={IP})", context.Connection.RemoteIpAddress);
        await _next(context);
    }

    private static async Task WriteProblemAsync(HttpContext ctx, int status, string code, string title, string detail)
    {
        if (status == StatusCodes.Status401Unauthorized)
            ctx.Response.Headers["WWW-Authenticate"] = "ApiKey";

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        string cid;
        if (ctx.Items.TryGetValue(HeaderNames.CorrelationId, out var v) &&
            v is string s && !string.IsNullOrWhiteSpace(s))
        {
            cid = s;
        }
        else
        {
            cid = ctx.TraceIdentifier;
        }


        var pd = new ProblemDetails
        {
            Type = $"urn:problem:{code}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = ctx.Request.Path
        };
        pd.Extensions["code"] = code;
        pd.Extensions["traceId"] = cid;

        await ctx.Response.WriteAsJsonAsync(pd);
    }
}
