using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DigiTekShop.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private static readonly string[] SensitiveKeywords = new[]
    {
        "Login","Register","Password","Token","Secret","Otp","Mfa","Reset","Refresh"
    };

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sensitive = SensitiveKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (sensitive)
            _logger.LogInformation("Handling {Request} (sensitive payload hidden)", name);
        else
            _logger.LogInformation("Handling {Request} => {@Payload}", name, request);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation("Handled {Request} in {Elapsed} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error handling {Request} after {Elapsed} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}