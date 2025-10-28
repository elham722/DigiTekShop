using DigiTekShop.Contracts.DTOs.RateLimit;

namespace DigiTekShop.Contracts.Abstractions.Caching;

public interface IRateLimiter
{
    Task<RateLimitDecision> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default);
}