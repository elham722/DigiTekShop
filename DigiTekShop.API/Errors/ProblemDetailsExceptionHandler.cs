using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // برای DbUpdateConcurrencyException
using Microsoft.IdentityModel.Tokens; // برای SecurityTokenException

namespace DigiTekShop.API.Errors;

public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Unhandled exception, TraceId={TraceId}", http.TraceIdentifier);

        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
        http.Response.ContentType = "application/problem+json";

        ProblemDetails pd;
        int status;

        // 1) DomainException → از کاتالوگ
        if (ex is DomainException dex)
        {
            var info = ErrorCatalog.Resolve(dex.Code);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? (dex.Message ?? info.DefaultMessage) : info.DefaultMessage, http);

            if (dex is DigiTekShop.SharedKernel.Exceptions.Validation.DomainValidationException dvx && dvx.Errors.Any())
            {
                pd.Extensions["errors"] = new Dictionary<string, string[]>
                {
                    ["validation"] = dvx.Errors.ToArray()
                };
            }
            if (dex.Metadata is { Count: > 0 })
                pd.Extensions["meta"] = dex.Metadata;

            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 2) FluentValidation → 422 VALIDATION_FAILED
        if (ex is ValidationException vex)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.VALIDATION_FAILED);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? vex.Message : info.DefaultMessage, http);

            var errors = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            if (errors.Count > 0)
                pd.Extensions["errors"] = errors;

            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 3) Concurrency
        if (ex is DbUpdateConcurrencyException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.CONCURRENCY_CONFLICT);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 4) Security token/Auth
        if (ex is SecurityTokenException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.UNAUTHORIZED);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 5) Unauthorized / NotFound / Timeout (نمونه‌هایی که گذاشته بودی)
        if (ex is UnauthorizedAccessException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.UNAUTHORIZED);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        if (ex is KeyNotFoundException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.NOT_FOUND);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        if (ex is TimeoutException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.TIMEOUT);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 6) لغو درخواست (کلاینت قطع کرد)
        if (ex is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
        {
            // 499 Client Closed Request (غیررسمی، ولی مرسوم) — اگر نمی‌خوای، 408 بزن
            status = 499;
            pd = Create("CLIENT_CLOSED_REQUEST", status, env.IsDevelopment() ? "Client cancelled the request." : "Request was cancelled.", http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }

        // 7) پیش‌فرض → INTERNAL_ERROR
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.INTERNAL_ERROR);
            status = info.HttpStatus;
            pd = Create(info.Code, status, env.IsDevelopment() ? ex.Message : info.DefaultMessage, http);
            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }
    }

    private static ProblemDetails Create(string code, int status, string detail, HttpContext http)
        => new()
        {
            Type = $"urn:problem:{code}",
            Title = code,
            Status = status,
            Detail = detail,
            Instance = http.Request.Path.Value,
            Extensions =
            {
                ["traceId"] = http.TraceIdentifier
            }
        };
}
