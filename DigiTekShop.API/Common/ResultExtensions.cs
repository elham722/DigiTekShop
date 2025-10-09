
using DigiTekShop.API.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Common;

public static class ResultToActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase c, Result<T> result, int okStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return c.StatusCode(okStatus, new ApiResponse<T>(result.Value!, c.HttpContext.TraceIdentifier));

        var (status, title) = MapErrorCodeToStatus(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext, status, title, "در پردازش درخواست خطایی رخ داد.", result.Errors);
        return c.StatusCode(status, pd);
    }

    public static IActionResult ToActionResult(this ControllerBase c, Result result, int okStatus = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess) return c.StatusCode(okStatus);

        var (status, title) = MapErrorCodeToStatus(result.ErrorCode);
        var pd = BuildProblemDetails(c.HttpContext, status, title, "در پردازش درخواست خطایی رخ داد.", result.Errors);
        return c.StatusCode(status, pd);
    }

    private static (int Status, string Title) MapErrorCodeToStatus(string? code) => code switch
    {
        "UNAUTHORIZED" or "AUTH_FAILED" => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED"),
        "FORBIDDEN" => (StatusCodes.Status403Forbidden, "FORBIDDEN"),
        "NOT_FOUND" => (StatusCodes.Status404NotFound, "NOT_FOUND"),
        "VALIDATION_ERROR" => (StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR"),
        "CONFLICT" or "EMAIL_TAKEN" => (StatusCodes.Status409Conflict, "CONFLICT"),
        "RATE_LIMIT" or "RATE_LIMIT_EXCEEDED" => (StatusCodes.Status429TooManyRequests, "RATE_LIMIT_EXCEEDED"),
        "TIMEOUT" => (StatusCodes.Status408RequestTimeout, "TIMEOUT"),
        "INTERNAL_ERROR" => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR"),
        _ => (StatusCodes.Status400BadRequest, "OPERATION_FAILED")
    };


    private static ProblemDetails BuildProblemDetails(HttpContext http, int status, string code, string userFacingDetail, IEnumerable<string>? errors)
    {
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var pd = new ProblemDetails
        {
            Title = code,
            Status = status,
            Detail = env.IsDevelopment() ? userFacingDetail : userFacingDetail,
            Instance = http.TraceIdentifier
        };
        if (errors is not null && errors.Any())
        {
            var grouped = errors
                .Select(e =>
                {
                    var idx = e.IndexOf(':');
                    if (idx > 0)
                        return new { Field = e[..idx].Trim(), Message = e[(idx + 1)..].Trim() };
                    return new { Field = "general", Message = e };
                })
                .GroupBy(x => x.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

            pd.Extensions["errors"] = grouped;
        }
        return pd;
    }
}
