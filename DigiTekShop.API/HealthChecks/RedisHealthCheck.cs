using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace DigiTekShop.API.HealthChecks;

/// <summary>
/// Custom health check for Redis connectivity
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Test write
            var testKey = "health:check:ping";
            await db.StringSetAsync(testKey, DateTime.UtcNow.ToString("O"), TimeSpan.FromSeconds(10));
            
            // Test read
            var value = await db.StringGetAsync(testKey);
            
            if (!value.HasValue)
            {
                _logger.LogWarning("Redis health check failed: Unable to read test key");
                return HealthCheckResult.Degraded("Redis read/write test failed");
            }

            // Get Redis info
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var info = await server.InfoAsync();

            var data = new Dictionary<string, object>
            {
                { "status", "Connected" },
                { "endpoints", string.Join(", ", endpoints.Select(e => e.ToString())) },
                { "connected_clients", server.ClientList().Length },
                { "uptime_seconds", info.FirstOrDefault(i => i.Key == "Server")?.FirstOrDefault(s => s.Key == "uptime_in_seconds").Value ?? "N/A" }
            };

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}

