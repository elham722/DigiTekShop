using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Errors
{
    public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ProblemDetailsExceptionHandler> _logger;
        public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) => _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
        {
            _logger.LogError(ex, "Unhandled exception, TraceId={TraceId}", http.TraceIdentifier);

            var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
            ProblemDetails pd;
            int status;

            // 1) DomainException → از کاتالوگ
            if (ex is DomainException dex)
            {
                var info = ErrorCatalog.Resolve(dex.Code);
                status = info.HttpStatus;
                pd = new ProblemDetails
                {
                    Title = info.Code,
                    Status = status,
                    Detail = env.IsDevelopment() ? (dex.Message ?? info.DefaultMessage) : info.DefaultMessage,
                    Instance = http.TraceIdentifier
                };

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
                status = StatusCodes.Status422UnprocessableEntity;
                pd = new ProblemDetails
                {
                    Title = ErrorCodes.Common.ValidationFailed,
                    Status = status,
                    Detail = env.IsDevelopment() ? vex.Message : "Validation failed.",
                    Instance = http.TraceIdentifier
                };

                var errors = vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                if (errors.Count > 0)
                    pd.Extensions["errors"] = errors;

                http.Response.StatusCode = status;
                await http.Response.WriteAsJsonAsync(pd, ct);
                return true;
            }

            // 3) دیگر استثناهای شناخته‌شده
            if (ex is UnauthorizedAccessException)
            {
                var info = ErrorCatalog.Resolve(ErrorCodes.Common.Unauthorized);
                status = info.HttpStatus;
                pd = new ProblemDetails
                {
                    Title = info.Code,
                    Status = status,
                    Detail = env.IsDevelopment() ? ex.Message : info.DefaultMessage,
                    Instance = http.TraceIdentifier
                };
                http.Response.StatusCode = status;
                await http.Response.WriteAsJsonAsync(pd, ct);
                return true;
            }

            if (ex is KeyNotFoundException)
            {
                var info = ErrorCatalog.Resolve(ErrorCodes.Common.NotFound);
                status = info.HttpStatus;
                pd = new ProblemDetails
                {
                    Title = info.Code,
                    Status = status,
                    Detail = env.IsDevelopment() ? ex.Message : info.DefaultMessage,
                    Instance = http.TraceIdentifier
                };
                http.Response.StatusCode = status;
                await http.Response.WriteAsJsonAsync(pd, ct);
                return true;
            }

            if (ex is TimeoutException)
            {
                var info = ErrorCatalog.Resolve(ErrorCodes.Common.Timeout);
                status = info.HttpStatus;
                pd = new ProblemDetails
                {
                    Title = info.Code,
                    Status = status,
                    Detail = env.IsDevelopment() ? ex.Message : info.DefaultMessage,
                    Instance = http.TraceIdentifier
                };
                http.Response.StatusCode = status;
                await http.Response.WriteAsJsonAsync(pd, ct);
                return true;
            }

            // 4) پیش‌فرض → INTERNAL_ERROR
            {
                var info = ErrorCatalog.Resolve(ErrorCodes.Common.InternalError);
                status = info.HttpStatus;
                pd = new ProblemDetails
                {
                    Title = info.Code,
                    Status = status,
                    Detail = env.IsDevelopment() ? ex.Message : info.DefaultMessage,
                    Instance = http.TraceIdentifier
                };
                http.Response.StatusCode = status;
                await http.Response.WriteAsJsonAsync(pd, ct);
                return true;
            }
        }
    }
}
