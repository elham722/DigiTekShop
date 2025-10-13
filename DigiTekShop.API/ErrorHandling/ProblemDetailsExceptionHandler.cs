using System.Diagnostics;
using DigiTekShop.API.Common.Http;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

       
        if (ex is DomainException dex)
        {
            var info = ErrorCatalog.Resolve(dex.Code) ?? ErrorCatalog.Resolve(ErrorCodes.Common.INTERNAL_ERROR)!;
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
            await WriteAsync(http, info.HttpStatus, info.Title ?? info.Code, detail, extras, ct);
            return true;
        }

        
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

      
        if (ex is DbUpdateConcurrencyException)
        {
            var info = ErrorCatalog.Resolve(ErrorCodes.Common.CONCURRENCY_CONFLICT)!;
            var extras = new Dictionary<string, object?> { ["traceId"] = traceId, ["code"] = info.Code };
            var detail = _env.IsDevelopment() ? ex.Message : info.DefaultMessage;
            await WriteAsync(http, info.HttpStatus, info.Title ?? "Concurrency conflict", detail, extras, ct);
            return true;
        }

       
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

        
        string codeId;
        if (extras != null && extras.TryGetValue("code", out var codeObj) && codeObj is not null)
            codeId = codeObj.ToString()!;
        else
            codeId = status.ToString();

        var pd = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"urn:problem:{codeId}",
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


}
