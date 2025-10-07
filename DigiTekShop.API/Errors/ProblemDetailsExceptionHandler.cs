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

            if (ex is FluentValidation.ValidationException vex)
            {
                var errors = vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                var problem = new ValidationProblemDetails(errors)
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = http.TraceIdentifier
                };

                http.Response.StatusCode = problem.Status.Value;
                await http.Response.WriteAsJsonAsync(problem, ct);
                return true;
            }

            var status = ex switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var pd = new ProblemDetails
            {
                Title = status == 500 ? "Unexpected error" : ex.GetType().Name,
                Detail = ex.Message,
                Status = status,
                Instance = http.TraceIdentifier
            };

            http.Response.StatusCode = status;
            await http.Response.WriteAsJsonAsync(pd, ct);
            return true;
        }
    }

}
