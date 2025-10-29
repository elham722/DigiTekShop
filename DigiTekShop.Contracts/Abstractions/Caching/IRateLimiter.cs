using DigiTekShop.Contracts.DTOs.RateLimit;

namespace DigiTekShop.Contracts.Abstractions.Caching;

public interface IRateLimiter
{
    Task<RateLimitDecision> ShouldAllowAsync(
        string key, int limit, TimeSpan window, CancellationToken ct = default);

    Task ResetBucketAsync(string key, CancellationToken ct = default);

    Task<RateLimitDecision> PeekAsync(
        string key, int limit, TimeSpan window, CancellationToken ct = default);
}