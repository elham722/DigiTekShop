namespace DigiTekShop.Contracts.Interfaces.Caching;

/// <summary>
/// Abstraction for distributed caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a value from cache
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores a value in cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="ttl">Time to live</param>
    /// <param name="ct">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="ct">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken ct = default);
}