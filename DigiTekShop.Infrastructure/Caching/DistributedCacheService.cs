using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Caching;

public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly JsonSerializerOptions _json;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, _json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache deserialization failed for key {Key}", key);
            return default;
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
        => SetAsync(key, value, absoluteTtl: ttl, slidingTtl: null, ct);

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteTtl, TimeSpan? slidingTtl, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);

        var opts = new DistributedCacheEntryOptions();
        if (absoluteTtl.HasValue) opts.AbsoluteExpirationRelativeToNow = absoluteTtl;
        if (slidingTtl.HasValue) opts.SlidingExpiration = slidingTtl;

        await _cache.SetAsync(key, bytes, opts, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => (await _cache.GetAsync(key, ct)) is not null;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var created = await factory();
        await SetAsync(key, created, ttl, ct);
        return created;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);
}
