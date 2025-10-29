namespace DigiTekShop.Infrastructure.Caching;

using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.Contracts.DTOs.RateLimit;
using StackExchange.Redis;

public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IDatabase _db;

    private const string AllowScript = @"
-- KEYS[1] = bucket key
-- ARGV[1] = window_ms
local count = redis.call('INCR', KEYS[1])
if count == 1 then
  redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
local ttl = redis.call('PTTL', KEYS[1])
return {count, ttl}
";

    private const string PeekScript = @"
-- KEYS[1] = bucket key
-- اگر وجود دارد، شمارش را بده (یا 0 اگر نه) و TTL
local exists = redis.call('EXISTS', KEYS[1])
if exists == 0 then
  return {0, -2}
end
local val = redis.call('GET', KEYS[1])
local ttl = redis.call('PTTL', KEYS[1])
return {tonumber(val), ttl}
";

    public RedisRateLimiter(IConnectionMultiplexer mux)
    {
        _mux = mux;
        _db = mux.GetDatabase();
    }

    public async Task<RateLimitDecision> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        var windowMs = (long)window.TotalMilliseconds;

        // ✅ استفاده‌ی مستقیم از اسکریپت (نه LoadedLuaScript)
        var res = (RedisResult[])await _db.ScriptEvaluateAsync(
            AllowScript,
            keys: new RedisKey[] { key },
            values: new RedisValue[] { windowMs });

        var count = (long)res[0];
        var ttlMs = (long)res[1];

        var ttl = ttlMs >= 0 ? TimeSpan.FromMilliseconds(ttlMs) : window;
        var resetAt = DateTimeOffset.UtcNow.Add(ttl);
        var allowed = count <= limit;

        return new RateLimitDecision(
            Allowed: allowed,
            Count: count,
            Limit: limit,
            Window: window,
            ResetAt: resetAt,
            Ttl: ttlMs >= 0 ? ttl : null
        );
    }

    public async Task<RateLimitDecision> PeekAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        var res = (RedisResult[])await _db.ScriptEvaluateAsync(
            PeekScript,
            keys: new RedisKey[] { key },
            values: Array.Empty<RedisValue>());

        var count = (long)res[0];
        var ttlMs = (long)res[1];

        var ttl = ttlMs >= 0 ? TimeSpan.FromMilliseconds(ttlMs) : window;
        var resetAt = DateTimeOffset.UtcNow.Add(ttl);
        var allowed = (count + 1) <= limit;

        return new RateLimitDecision(
            Allowed: allowed,
            Count: count,
            Limit: limit,
            Window: window,
            ResetAt: resetAt,
            Ttl: ttlMs >= 0 ? ttl : null
        );
    }

    public async Task ResetBucketAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(key);
    }
}
