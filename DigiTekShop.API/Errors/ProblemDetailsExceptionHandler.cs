using DigiTekShop.API.Models;
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

            var (status, code, detail) = ex switch
            {
                FluentValidation.ValidationException vex => (400, "VALIDATION_ERROR", "Validation failed"),
                UnauthorizedAccessException => (401, "UNAUTHORIZED", "Unauthorized"),
                KeyNotFoundException => (404, "NOT_FOUND", "Resource not found"),
                TimeoutException => (408, "TIMEOUT", "Operation timed out"),
                _ => (500, "INTERNAL_ERROR", "An unexpected error occurred")
            };

            var pd = new ProblemDetails
            {
                Title = code,
                Status = status,
                Detail = http.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? ex.Message : detail,
                Instance = http.TraceIdentifier
            };

            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }
    }

}
