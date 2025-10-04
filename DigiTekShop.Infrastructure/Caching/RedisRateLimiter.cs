using DigiTekShop.Contracts.Interfaces.Caching;
using StackExchange.Redis;

namespace DigiTekShop.Infrastructure.Caching;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _mux;
    public RedisRateLimiter(IConnectionMultiplexer mux) => _mux = mux;

    public async Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)window.TotalSeconds;
        var redisKey = $"rl:{key}:{bucket}";

        try
        {
            var count = await db.StringIncrementAsync(redisKey);
            if (count == 1)
                await db.KeyExpireAsync(redisKey, window);

            return count <= limit;
        }
        catch
        {
            return true;
        }
    }
}