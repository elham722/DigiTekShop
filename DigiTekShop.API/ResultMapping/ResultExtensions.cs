#nullable enable
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using DigiTekShop.SharedKernel.Http;
using static DigiTekShop.SharedKernel.Http.RateLimitMetaKeys;
using System.Diagnostics;
using HeaderNames = DigiTekShop.API.Common.Http.HeaderNames;

namespace DigiTekShop.API.ResultMapping;

public static class ResultToActionResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase c,
        Result<T> result,
        int okStatus = StatusCodes.Status200OK,
        Func<T, string?>? createdLocationFactory = null)
    {
        if (result.IsSuccess)
        {
            var traceId = GetTraceId(c.HttpContext);
            var payload = result.Value;

            // Set Rate Limit headers if present
            MaybeSetRateLimitHeaders(c.HttpContext, result.Metadata);

            if (createdLocationFactory is not null && payload is not null)
            {
                var location = createdLocationFactory(payload);
                if (!string.IsNullOrWhiteSpace(location))
                    return c.Created(location, new ApiResponse<T?>(payload, TraceId: traceId, Timestamp: DateTimeOffset.UtcNow));
            }

            return c.StatusCode(okStatus, new ApiResponse<T?>(payload, TraceId: traceId, Timestamp: DateTimeOffset.UtcNow));
        }

        // Set Rate Limit headers if present (for failures)
        MaybeSetRateLimitHeaders(c.HttpContext, result.Metadata);

        var info = ErrorCatalog.Resolve(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, info.HttpStatus, info.Code, info.DefaultMessage, result.Errors);
        return c.StatusCode(info.HttpStatus, pd).WithProblemContentType();
    }

    public static IActionResult ToActionResult(this ControllerBase c, Result result, int okStatus = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess) 
        {
            // Set Rate Limit headers if present
            MaybeSetRateLimitHeaders(c.HttpContext, result.Metadata);
            return c.StatusCode(okStatus);
        }

        // Set Rate Limit headers if present (for failures)
        MaybeSetRateLimitHeaders(c.HttpContext, result.Metadata);

        var info = ErrorCatalog.Resolve(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, info.HttpStatus, info.Code, info.DefaultMessage, result.Errors);
        return c.StatusCode(info.HttpStatus, pd).WithProblemContentType();
    }

    #region Helpers

    private static string GetTraceId(HttpContext http) =>
        Activity.Current?.Id
        ?? (http.Items.TryGetValue(HeaderNames.CorrelationId, out var cid) && cid is string s && !string.IsNullOrWhiteSpace(s) ? s : null)
        ?? http.TraceIdentifier
        ?? Guid.NewGuid().ToString();

    private static void MaybeSetRateLimitHeaders(HttpContext http, IReadOnlyDictionary<string, object?>? meta)
    {
        if (meta is null) return;
        if (!meta.TryGetValue(RateLimitMetaKeys.Limit, out var limitObj)) return;

        int limit = Convert.ToInt32(limitObj);
        int remaining = meta.TryGetValue(RateLimitMetaKeys.Remaining, out var remObj) ? Convert.ToInt32(remObj) : 0;
        int windowSeconds = meta.TryGetValue(RateLimitMetaKeys.WindowSeconds, out var winObj) ? Convert.ToInt32(winObj) : 0;
        long resetUnix = meta.TryGetValue(RateLimitMetaKeys.ResetAtUnix, out var rstObj) ? Convert.ToInt64(rstObj) : 0;
        string policy = meta.TryGetValue(RateLimitMetaKeys.Policy, out var polObj) ? polObj?.ToString() ?? "default" : "default";

        http.Response.Headers["X-RateLimit-Policy"] = policy;
        http.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        http.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        http.Response.Headers["X-RateLimit-Window"] = windowSeconds.ToString();
        http.Response.Headers["X-RateLimit-Reset"] = resetUnix.ToString();
        http.Response.Headers["X-RateLimit-Scope"] = "user";

        var retryAfter = Math.Max(0, (int)Math.Ceiling((DateTimeOffset.FromUnixTimeSeconds(resetUnix) - DateTimeOffset.UtcNow).TotalSeconds));
        http.Response.Headers["Retry-After"] = retryAfter.ToString();

        http.Response.Headers["Cache-Control"] = "no-store, max-age=0";
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails BuildProblemDetails(
        HttpContext http, int status, string errorCode, string defaultMessage, IEnumerable<string>? errors)
    {
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var path = http.Request.Path.HasValue ? http.Request.Path.Value : null;
        var traceId = GetTraceId(http);

        var title = status switch
        {
            StatusCodes.Status400BadRequest => "The request is invalid.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "You don't have access to this resource.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "A conflict occurred.",
            StatusCodes.Status422UnprocessableEntity => "The request could not be processed.",
            StatusCodes.Status429TooManyRequests => "Too Many Requests", 
            _ => "An error occurred."
        };

        var detail = defaultMessage;

        if (env.IsDevelopment() && errors is not null)
        {
            var first = errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
            if (!string.IsNullOrWhiteSpace(first))
                detail = first!;
        }

        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"urn:problem:{errorCode.ToLowerInvariant()}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = path
        };

        pd.Extensions["traceId"] = traceId;
        pd.Extensions["code"] = errorCode;
        pd.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (errors is not null)
        {
            var grouped = errors
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e =>
                {
                    var idx = e.IndexOf(':');
                    if (idx > 0)
                        return new { Field = e[..idx].Trim(), Message = e[(idx + 1)..].Trim() };
                    return new { Field = "general", Message = e.Trim() };
                })
                .GroupBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

            if (grouped.Count > 0)
                pd.Extensions["errors"] = grouped;
        }

        return pd;
    }

    private static IActionResult WithProblemContentType(this IActionResult result)
    {
        if (result is ObjectResult or)
        {
            or.ContentTypes.Clear();
            or.ContentTypes.Add("application/problem+json");
        }
        return result;
    }

    #endregion
}
