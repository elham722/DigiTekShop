using StackExchange.Redis;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<RedisRateLimiter> _log;
    private const string Prefix = "dts:rl:";

    private const string SlidingLua = @"
        redis.call('ZREMRANGEBYSCORE', KEYS[1], 0, ARGV[1] - ARGV[2])
        redis.call('ZADD', KEYS[1], ARGV[1], ARGV[1])
        local count = redis.call('ZCARD', KEYS[1])
        redis.call('PEXPIRE', KEYS[1], ARGV[2])
        return count
    ";

    public RedisRateLimiter(IConnectionMultiplexer mux, ILogger<RedisRateLimiter> log)
    { _mux = mux; _log = log; }

    public async Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        if (limit <= 0) return false;
        if (window <= TimeSpan.Zero) return true;

        try
        {
            var db = _mux.GetDatabase();
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowMs = (long)window.TotalMilliseconds;

            var redisKey = (RedisKey)$"{Prefix}{{{key}}}";
            var result = await db.ScriptEvaluateAsync(
                SlidingLua,
                keys: new RedisKey[] { redisKey },
                values: new RedisValue[] { nowMs, windowMs, limit });

            var count = (long)result;
            var allowed = count <= limit;

            if (!allowed)
                _log.LogDebug("Rate limit exceeded: key={Key}, count={Count}, limit={Limit}", key, count, limit);

            return allowed;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "RateLimiter fail-open for key {Key}", key);
            return true;
        }
    }
}
