namespace DigiTekShop.Infrastructure.Caching;

using DigiTekShop.Contracts.Abstractions.Caching;
using DigiTekShop.SharedKernel.Http;
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
-- اگر TTL -1 باشه (بدون TTL)، window_ms رو برگردون
if ttl == -1 then
  ttl = ARGV[1]
end
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

        // TTL: -1 = کلید بدون TTL (نباید پیش بیاد چون PEXPIRE زدیم), -2 = کلید وجود نداره
        // اگر TTL معتبر نیست، از window استفاده کن
        TimeSpan ttl;
        if (ttlMs > 0)
        {
            ttl = TimeSpan.FromMilliseconds(ttlMs);
        }
        else if (ttlMs == -1)
        {
            // کلید بدون TTL - احتمالاً PEXPIRE fail شده - از window استفاده کن
            ttl = window;
        }
        else // ttlMs == -2 یا منفی
        {
            // کلید وجود نداره - باید window باشه
            ttl = window;
        }

        var now = DateTimeOffset.UtcNow;
        var resetAt = now.Add(ttl);
        
        // مطمئن شو resetAt همیشه در آینده است (حداقل 1 ثانیه)
        if (resetAt <= now)
            resetAt = now.AddSeconds(1);
        
        var allowed = count <= limit;

        return new RateLimitDecision(
            Allowed: allowed,
            Count: count,
            Limit: limit,
            Window: window,
            ResetAt: resetAt,
            Ttl: ttlMs > 0 ? ttl : null
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
