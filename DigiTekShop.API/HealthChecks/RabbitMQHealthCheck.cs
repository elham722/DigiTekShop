using DigiTekShop.Contracts.Options.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DigiTekShop.API.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQHealthCheck> _logger;
    private readonly RabbitMqOptions _options;

    public RabbitMQHealthCheck(
        IConnection connection,
        ILogger<RabbitMQHealthCheck> logger,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection is null || !_connection.IsOpen)
            {
                _logger.LogWarning("RabbitMQ health: connection is not open.");
                return HealthCheckResult.Unhealthy("RabbitMQ connection is not open");
            }

            // v7+: کانال async
            await using var channel = await _connection.CreateChannelAsync(null, cancellationToken);

            // تست سبک کانال (بدون تغییر topology)
            await channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken);

            var data = new Dictionary<string, object?>
            {
                ["status"] = "Connected",
                ["endpoint"] = _connection.Endpoint?.ToString(),
                ["isOpen"] = _connection.IsOpen,
                ["hostname"] = _options.HostName,
                ["vhost"] = _options.VirtualHost
            };

            return HealthCheckResult.Healthy("RabbitMQ is healthy", data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("RabbitMQ check cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
        }
    }
}
