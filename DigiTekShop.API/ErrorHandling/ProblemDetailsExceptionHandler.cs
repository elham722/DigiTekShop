#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using HeaderNames = DigiTekShop.API.Common.Http.HeaderNames;

namespace DigiTekShop.API.ErrorHandling;

public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public ProblemDetailsExceptionHandler(
        ILogger<ProblemDetailsExceptionHandler> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        if (http.Response.HasStarted) return false;

        var traceId =
            (http.Items.TryGetValue(HeaderNames.CorrelationId, out var cid) && cid is string s && !string.IsNullOrWhiteSpace(s))
                ? s
                : (Activity.Current?.Id ?? http.TraceIdentifier);

        if (ex is ValidationException or DomainException)
            _logger.LogWarning(ex, "Handled exception (traceId={TraceId})", traceId);
        else
            _logger.LogError(ex, "Unhandled exception (traceId={TraceId})", traceId);

        // --- DomainException: با پوشش هدرهای RateLimit ---
        if (ex is DomainException dex)
        {
            var info = ErrorCatalog.Resolve(dex.Code) ?? ErrorCatalog.Resolve(ErrorCodes.Common.INTERNAL_ERROR)!;

            if (IsRateLimitCode(dex.Code) && dex.Metadata is { Count: > 0 })
            {
                int limit = TryGet<int>(dex.Metadata, RateLimitedException.Meta.Limit);
                int remaining = Math.Max(0, TryGet<int>(dex.Metadata, RateLimitedException.Meta.Remaining));
                int windowSecs = Math.Max(1, TryGet<int>(dex.Metadata, RateLimitedException.Meta.WindowSeconds));
                long resetUnix = TryGet<long>(dex.Metadata, RateLimitedException.Meta.ResetAtUnix);
                var policy = TryGet<string>(dex.Metadata, RateLimitedException.Meta.Policy) ?? "default";

                http.Response.Headers["X-RateLimit-Policy"] = policy;
                http.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
                http.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                http.Response.Headers["X-RateLimit-Reset"] = resetUnix.ToString();
                http.Response.Headers["X-RateLimit-Window"] = windowSecs.ToString();

                var retryAfter = Math.Max(0, (int)(DateTimeOffset.FromUnixTimeSeconds(resetUnix) - DateTimeOffset.UtcNow).TotalSeconds);
                http.Response.Headers["Retry-After"] = retryAfter.ToString();
                http.Response.Headers["Cache-Control"] = "no-store, max-age=0";
            }

            var extras = new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
                ["code"] = info.Code
            };

            if (dex is SharedKernel.Exceptions.Validation.DomainValidationException dvx && dvx.Errors.Any())
                extras["errors"] = new Dictionary<string, string[]>
                {
                    ["validation"] = dvx.Errors.ToArray()
                };

            if (dex.Metadata is { Count: > 0 })
                extras["meta"] = dex.Metadata;

            var detail = _env.IsDevelopment() ? (dex.Message ?? info.DefaultMessage) : info.DefaultMessage;

            var title = info.Title ?? (info.HttpStatus == StatusCodes.Status429TooManyRequests
                ? "Too Many Requests"
                : info.Code);

            await WriteAsync(http, info.HttpStatus, title, detail, extras, ct);
            return true;
        }

        // --- FluentValidation ---
        if (ex is ValidationException vex)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.VALIDATION_FAILED)!;
            var errors = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var extras = new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
                ["code"] = info.Code,
                ["errors"] = errors
            };

            var detail = _env.IsDevelopment() ? vex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, info.Title ?? "Validation failed", detail, extras, ct);
            return true;
        }

        // --- Concurrency ---
        if (ex is DbUpdateConcurrencyException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.CONCURRENCY_CONFLICT)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, info.Title ?? "Concurrency conflict", detail, extras, ct);
            return true;
        }

        // --- JWT ---
        if (ex is SecurityTokenExpiredException)
        {
            http.Response.Headers["WWW-Authenticate"] =
                "Bearer error=\"invalid_token\", error_description=\"token expired\"";
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.UNAUTHORIZED)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = "TOKEN_EXPIRED" };
            var detail = _env.IsDevelopment() ? ex.Message : "Authentication token expired.";
            await WriteAsync(http, info.HttpStatus, "Unauthorized", detail, extras, ct);
            return true;
        }
        if (ex is SecurityTokenException)
        {
            http.Response.Headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\"";
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.UNAUTHORIZED)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, "Unauthorized", detail, extras, ct);
            return true;
        }

        // --- سایر موارد ---
        if (ex is UnauthorizedAccessException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.FORBIDDEN) ?? new ErrorInfo("FORBIDDEN", 403, "Forbidden");
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, "Forbidden", detail, extras, ct);
            return true;
        }

        if (ex is KeyNotFoundException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.NOT_FOUND)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, "Not Found", detail, extras, ct);
            return true;
        }

        if (ex is TimeoutException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.TIMEOUT)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, "Timeout", detail, extras, ct);
            return true;
        }

        if (ex is OperationCanceledException)
        {
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = "REQUEST_CANCELED" };
            if (http.RequestAborted.IsCancellationRequested)
            {
                await WriteAsync(http, 499, "Client Closed Request",
                    _env.IsDevelopment() ? "Client cancelled the request." : "Request was cancelled.", extras, ct);
                return true;
            }
            await WriteAsync(http, 408, "Request Timeout",
                _env.IsDevelopment() ? "Request was canceled by the server." : "Request timed out.", extras, ct);
            return true;
        }

        // --- Fallback 500 ---
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.INTERNAL_ERROR)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, info.Title ?? "Internal Server Error", detail, extras, ct);
            return true;
        }
    }

    private static async Task WriteAsync(
        HttpContext http,
        int status,
        string title,
        string detail,
        IDictionary<string, object?> extras,
        CancellationToken ct)
    {
        http.Response.StatusCode = status;
        http.Response.ContentType = "application/problem+json";

        var codeId =
            (extras != null && extras.TryGetValue("code", out var codeObj) && codeObj is not null)
                ? codeObj.ToString()!
                : status.ToString();

        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"urn:problem:{codeId.ToLowerInvariant()}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = http.Request.Path.Value
        };

        if (extras != null)
        {
            foreach (var kv in extras)
                pd.Extensions[kv.Key] = kv.Value;
        }

        await http.Response.WriteAsJsonAsync(pd, ct);
    }

    private static bool IsRateLimitCode(string? code)
        => string.Equals(code, ErrorCodes.Common.RATE_LIMIT_EXCEEDED, StringComparison.Ordinal)
        || string.Equals(code, ErrorCodes.Otp.OTP_SEND_RATE_LIMITED, StringComparison.Ordinal)
        || string.Equals(code, ErrorCodes.Otp.OTP_VERIFY_RATE_LIMITED, StringComparison.Ordinal);

    // ✅ امضای متد اصلاح شد تا با IReadOnlyDictionary سازگار باشد
    private static T TryGet<T>(IReadOnlyDictionary<string, object?> meta, string key)
    {
        if (meta.TryGetValue(key, out var v))
        {
            if (v is T t) return t;
            try
            {
                if (v is not null) return (T)Convert.ChangeType(v, typeof(T));
            }
            catch { /* ignore */ }
        }
        return default!;
    }
}
