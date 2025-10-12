using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int DefaultThresholdMs = 1000;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();
        
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > DefaultThresholdMs)
        {
            _logger.LogWarning(
                "Performance issue: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName,
                stopwatch.ElapsedMilliseconds,
                DefaultThresholdMs);
        }

        return response;
    }
}