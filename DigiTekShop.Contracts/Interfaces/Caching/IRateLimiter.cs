namespace DigiTekShop.Contracts.Interfaces.Caching;

/// <summary>
/// Abstraction for rate limiting operations
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if a request should be allowed based on rate limiting rules
    /// </summary>
    /// <param name="key">Unique identifier for the rate limit (e.g., user ID, IP address)</param>
    /// <param name="limit">Maximum number of requests allowed</param>
    /// <param name="window">Time window for the limit</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if request should be allowed, false if rate limit exceeded</returns>
    Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default);
}