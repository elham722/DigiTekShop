using StackExchange.Redis;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Logging;
using DigiTekShop.Contracts.DTOs.RateLimit;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<RedisRateLimiter> _log;
    private const string Prefix = "dts:rl:";

    private const string Script = @"
local c = redis.call('INCR', KEYS[1])
if c == 1 then
  redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
return c
";

    public RedisRateLimiter(IConnectionMultiplexer mux, ILogger<RedisRateLimiter> log)
    { _mux = mux; _log = log; }

    public async Task<RateLimitDecision> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        if (limit <= 0) return new(false, 0, TimeSpan.Zero);
        if (window <= TimeSpan.Zero) return new(true, 0, null);

        try
        {
            var db = _mux.GetDatabase();
            var count = (long)await db.ScriptEvaluateAsync(Script, new RedisKey[] { $"{Prefix}{key}" },
                new RedisValue[] { (long)window.TotalMilliseconds });

            var ttl = await db.KeyTimeToLiveAsync($"{Prefix}{key}");
            return new(count <= limit, count, ttl);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "RateLimiter failed for key {Key}", key);
            return new(true, 0, null); // fail-open
        }
    }
}
