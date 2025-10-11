using DigiTekShop.API.Models;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Extensions;

public static class ResultToActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase c, Result<T> result, int okStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            var traceId = c.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
            // اگر T می‌تونه null باشه، اینجا هندل کن:
            var payload = result.Value is null ? default : result.Value;
            return c.StatusCode(okStatus, new ApiResponse<T?>(payload, TraceId: traceId));
        }

        var info = ErrorCatalog.Resolve(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, info.HttpStatus, info.Code, info.DefaultMessage, result.Errors);
        return c.StatusCode(info.HttpStatus, pd).WithProblemContentType();
    }

    public static IActionResult ToActionResult(this ControllerBase c, Result result, int okStatus = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess) return c.StatusCode(okStatus);

        var info = ErrorCatalog.Resolve(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, info.HttpStatus, info.Code, info.DefaultMessage, result.Errors);
        return c.StatusCode(info.HttpStatus, pd).WithProblemContentType();
    }

    private static ProblemDetails BuildProblemDetails(
        HttpContext http, int status, string code, string defaultMessage, IEnumerable<string>? errors)
    {
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var path = http.Request.Path.HasValue ? http.Request.Path.Value : null;
        var traceId = http.TraceIdentifier;

        string detail = defaultMessage;
        if (env.IsDevelopment() && errors is not null)
        {
            var first = errors.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) detail = first!;
        }

        var pd = new ProblemDetails
        {
            Type = $"urn:problem:{code}",
            Title = code,
            Status = status,
            Detail = detail,
            Instance = path // ← مسیر درخواست
        };

        // TraceId + errors در extensions
        pd.Extensions["traceId"] = traceId;

        if (errors is not null && errors.Any())
        {
            var grouped = errors
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e =>
                {
                    var idx = e.IndexOf(':');
                    if (idx > 0)
                        return new { Field = e[..idx].Trim(), Message = e[(idx + 1)..].Trim() };
                    return new { Field = "general", Message = e };
                })
                .GroupBy(x => x.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

            if (grouped.Count > 0)
                pd.Extensions["errors"] = grouped;
        }

        return pd;
    }

    /// <summary>Ensure RFC 7807 content-type.</summary>
    private static IActionResult WithProblemContentType(this IActionResult result)
    {
        if (result is ObjectResult or)
        {
            or.ContentTypes.Clear();
            or.ContentTypes.Add("application/problem+json");
        }
        return result;
    }
}
