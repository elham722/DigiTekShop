using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.IO.Compression;
using DigiTekShop.Contracts.Abstractions.Caching;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Caching;

public sealed class DistributedCacheService : ICacheService
{
    private const string Prefix = "dts:cache:";
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly IDistributedLockService? _lock;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        IDistributedLockService? @lock = null)
    {
        _cache = cache;
        _logger = logger;
        _lock = @lock;
    }

    private static string K(string key) => $"{Prefix}{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(K(key), ct);
        if (bytes is null) return default;

        try
        {
            var payload = Decompress(bytes);
            return JsonSerializer.Deserialize<T>(payload, _json);
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
        var json = JsonSerializer.SerializeToUtf8Bytes(value, _json);
        var bytes = Compress(json);

        var opts = new DistributedCacheEntryOptions();
        if (absoluteTtl.HasValue) opts.AbsoluteExpirationRelativeToNow = absoluteTtl;
        if (slidingTtl.HasValue) opts.SlidingExpiration = slidingTtl;

        await _cache.SetAsync(K(key), bytes, opts, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => (await _cache.GetAsync(K(key), ct)) is not null;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        var cacheKey = K(key);

        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        
        var lockKey = $"{cacheKey}:lock";
        string? lockToken = null;

        try
        {
            if (_lock is not null)
                lockToken = await _lock.AcquireAsync(lockKey, TimeSpan.FromSeconds(5), ct);

            
            cached = await GetAsync<T>(key, ct);
            if (cached is not null) return cached;

            
            var created = await factory();
            await SetAsync(key, created, ttl, ct);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrCreate failed for key {Key}. Falling back to factory.", key);
            return await factory(); 
        }
        finally
        {
            if (lockToken is not null && _lock is not null)
            {
                await _lock.ReleaseAsync(lockKey, lockToken, ct);
            }
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(K(key), ct);

    #region Helpers

    private static byte[] Compress(byte[] plain)
    {
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
            gz.Write(plain, 0, plain.Length);
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] input)
    {
        try
        {
            using var ms = new MemoryStream(input);
            using var gz = new GZipStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gz.CopyTo(outMs);
            return outMs.ToArray();
        }
        catch
        {
            return input;
        }
    }

    #endregion
}
