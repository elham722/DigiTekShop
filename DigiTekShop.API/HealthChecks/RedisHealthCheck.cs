using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using DigiTekShop.Contracts.Options.Caching;

namespace DigiTekShop.API.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly RedisOptions _options;

    public RedisHealthCheck(
        IConnectionMultiplexer mux,
        ILogger<RedisHealthCheck> logger,
        IOptions<RedisOptions> options)
    {
        _mux = mux;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_mux is null || !_mux.IsConnected)
            {
                _logger.LogWarning("Redis health: multiplexer not connected.");
                return HealthCheckResult.Unhealthy("Redis connection is not established");
            }

            var db = _mux.GetDatabase();

            var key = $"health_check_{Guid.NewGuid():N}";
            var value = DateTime.UtcNow.Ticks.ToString();

            await db.StringSetAsync(key, value, TimeSpan.FromSeconds(30));
            var got = await db.StringGetAsync(key);
            await db.KeyDeleteAsync(key);

            if (got != value)
            {
                _logger.LogWarning("Redis health: read/write mismatch.");
                return HealthCheckResult.Unhealthy("Redis read/write test failed");
            }

            var data = new Dictionary<string, object?>
            {
                ["status"] = "Connected",
                ["endpoint"] = _options.ConnectionString,
                ["isConnected"] = _mux.IsConnected,
                ["database"] = db.Database
            };

            // INFO اختیاری (ممکن است در managedها محدود باشد)
            try
            {
                var endpoints = _mux.GetEndPoints();
                if (endpoints.Length > 0)
                {
                    var server = _mux.GetServer(endpoints[0]);
                    data["serverVersion"] = server.Version?.ToString();

                    var info = await server.InfoAsync(); // ممکن است محدود باشد
                    var flat = info.SelectMany(s => s).ToDictionary(p => p.Key, p => p.Value);

                    if (flat.TryGetValue("used_memory_human", out var mem)) data["memoryUsage"] = mem;
                    if (flat.TryGetValue("connected_clients", out var cli)) data["connectedClients"] = cli;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Redis INFO not available; continuing with basic health.");
            }

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("Redis check cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}
