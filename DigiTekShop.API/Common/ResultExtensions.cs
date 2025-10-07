using DigiTekShop.API.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this ControllerBase c, Result r)
        => r.IsSuccess ? c.NoContent() : c.ToProblem(r);

    public static IActionResult ToActionResult<T>(this ControllerBase c, Result<T> r, object? meta = null)
        => r.IsSuccess
            ? c.Ok(new ApiResponse<T>(r.Value!, meta, c.HttpContext.TraceIdentifier, DateTimeOffset.UtcNow))
            : c.ToProblem(r);

    // --- Overload 1: Result (بدون جنریک)
    private static IActionResult ToProblem(this ControllerBase c, Result r)
    {
        var status = MapStatus(r.ErrorCode);
        var pd = new ProblemDetails
        {
            Title = "Request failed",
            Status = status,
            Detail = r.GetFirstError() ?? "Operation failed",
            Instance = c.HttpContext.TraceIdentifier
        };
        pd.Extensions["errors"] = r.Errors ?? Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(r.ErrorCode))
            pd.Extensions["errorCode"] = r.ErrorCode;

        return c.Problem(pd.Detail, statusCode: pd.Status, title: pd.Title);
    }

    // --- Overload 2: Result<T> (جنریک)
    private static IActionResult ToProblem<T>(this ControllerBase c, Result<T> r)
    {
        var status = MapStatus(r.ErrorCode);
        var pd = new ProblemDetails
        {
            Title = "Request failed",
            Status = status,
            Detail = r.GetFirstError() ?? "Operation failed",
            Instance = c.HttpContext.TraceIdentifier
        };
        pd.Extensions["errors"] = r.Errors ?? Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(r.ErrorCode))
            pd.Extensions["errorCode"] = r.ErrorCode;

        return c.Problem(pd.Detail, statusCode: pd.Status, title: pd.Title);
    }

    private static int MapStatus(string? errorCode) => errorCode switch
    {
        "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "CONFLICT" => StatusCodes.Status409Conflict,
        "RATE_LIMIT_EXCEEDED" => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status400BadRequest
    };
}
