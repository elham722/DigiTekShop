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
            return c.StatusCode(okStatus, new ApiResponse<T>(result.Value!, TraceId: traceId));
        }

        var (status, title, defaultMsg) = MapError(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, status, title, defaultMsg, result.Errors);
        return c.StatusCode(status, pd);
    }

    public static IActionResult ToActionResult(this ControllerBase c, Result result, int okStatus = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess) return c.StatusCode(okStatus);

        var (status, title, defaultMsg) = MapError(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext!, status, title, defaultMsg, result.Errors);
        return c.StatusCode(status, pd);
    }


    private static (int Status, string Title, string DefaultMessage) MapError(string? code)
    {
        var info = ErrorCatalog.Resolve(code);
        return (info.HttpStatus, info.Code, info.DefaultMessage);
    }

    private static ProblemDetails BuildProblemDetails(
        HttpContext http, int status, string code, string defaultMessage, IEnumerable<string>? errors)
    {
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();

        string detail = defaultMessage;
        if (env.IsDevelopment() && errors is not null)
        {
            var first = errors.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) detail = first;
        }

        var pd = new ProblemDetails
        {
            Title = code,
            Status = status,
            Detail = detail,
            Instance = http.TraceIdentifier
        };

        if (errors is not null && errors.Any())
        {
            var grouped = errors
                .Where(e => !string.IsNullOrWhiteSpace(e)) // ✅ فیلتر کردن null ها
                .Select(e =>
                {
                    var idx = e.IndexOf(':');
                    if (idx > 0)
                        return new { Field = e[..idx].Trim(), Message = e[(idx + 1)..].Trim() };
                    return new { Field = "general", Message = e };
                })
                .GroupBy(x => x.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

            if (grouped.Any())
                pd.Extensions["errors"] = grouped;
        }

        return pd;
    }

}
