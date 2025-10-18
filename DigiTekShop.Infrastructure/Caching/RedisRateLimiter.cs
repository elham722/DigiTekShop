using DigiTekShop.Contracts.Abstractions.Caching;
using StackExchange.Redis;

namespace DigiTekShop.Infrastructure.Caching
{
    public class RedisRateLimiter : IRateLimiter
    {
        private readonly IConnectionMultiplexer _mux;

        private static readonly LuaScript _script = LuaScript.Prepare(@"
                 local c = redis.call('INCR', @key)
                 if c == 1 then
                 redis.call('PEXPIRE', @key, @ttl)
                 end return c");

        public RedisRateLimiter(IConnectionMultiplexer mux) => _mux = mux;

        public async Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
        {
            if (limit <= 0) return false;
            if (window <= TimeSpan.Zero) return true;

            try
            {
                var db = _mux.GetDatabase();
                var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds;

                var redisKey = (RedisKey)$"rl:{{{key}}}:{bucket}";
                var ttlMs = (long)window.TotalMilliseconds;

                var result = await db.ScriptEvaluateAsync(
                    _script,
                    new { key = redisKey, ttl = ttlMs }
                );

                var count = (long)result;
                return count <= limit;
            }
            catch
            {
                return true; 
            }
        }
    }
}