using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly int _thresholdInMilliseconds;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, int thresholdInMilliseconds = 1000)
    {
        _logger = logger;
        _thresholdInMilliseconds = thresholdInMilliseconds;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();
        
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > _thresholdInMilliseconds)
        {
            _logger.LogWarning(
                "Performance issue detected: Request {RequestName} took {ElapsedMilliseconds}ms (threshold: {Threshold}ms)",
                requestName,
                stopwatch.ElapsedMilliseconds,
                _thresholdInMilliseconds);
        }

        return response;
    }
}