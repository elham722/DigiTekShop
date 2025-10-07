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

            var status = ex switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = new ProblemDetails
            {
                Title = status == 500 ? "خطای غیرمنتظره" : ex.GetType().Name,
                Detail = ex.Message,
                Status = status,
                Instance = http.TraceIdentifier
            };

            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(problem, ct);
            return true;
        }
    }
}
