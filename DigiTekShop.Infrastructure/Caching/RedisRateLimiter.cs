using StackExchange.Redis;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<RedisRateLimiter> _log;

    private const string Script = @"
        local c = redis.call('INCR', KEYS[1])
        if c == 1 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        return c
    ";

    private const string Prefix = "dts:rl:";

    public RedisRateLimiter(IConnectionMultiplexer mux, ILogger<RedisRateLimiter> log)
    {
        _mux = mux;
        _log = log;
    }

    public async Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        if (limit <= 0) return false;
        if (window <= TimeSpan.Zero) return true;

        try
        {
            var db = _mux.GetDatabase();
            var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds;

            var redisKey = (RedisKey)$"{Prefix}{{{key}}}:{bucket}";
            var ttlMs = (long)window.TotalMilliseconds;

            var result = await db.ScriptEvaluateAsync(
                Script,
                keys: new RedisKey[] { redisKey },
                values: new RedisValue[] { ttlMs }
            );

            var count = (long)result;
            var allowed = count <= limit;

            if (!allowed)
            {
                _log.LogDebug("Rate limit exceeded: key={Key}, count={Count}, limit={Limit}", key, count, limit);
            }

            return allowed;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "RateLimiter fail-open for key {Key}", key);
            return true; 
        }
    }
}