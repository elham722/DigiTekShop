using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace DigiTekShop.API.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext hc, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync(); 

            var key = "health:check:ping";
            var now = DateTime.UtcNow.ToString("O");
            await db.StringSetAsync(key, now, TimeSpan.FromSeconds(10));
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogWarning("Redis health: set/get failed.");
                return HealthCheckResult.Degraded("Redis read/write test failed");
            }

            var data = new Dictionary<string, object>
            {
                { "status", "Connected" },
                { "endpoints", string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString())) }
            };

            try
            {
                var ep = _redis.GetEndPoints();
                if (ep.Length > 0)
                {
                    var server = _redis.GetServer(ep[0]);
                    if (server != null && server.IsConnected)
                    {
                        data["isServerConnected"] = true;
                        
                    }
                }
            }
            catch (Exception inner)
            {
                _logger.LogDebug(inner, "Redis server info not available (non-admin).");
            }

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}


