namespace DigiTekShop.Contracts.Abstractions.Caching;

public interface IRateLimiter
{
    Task<bool> ShouldAllowAsync(string key, int limit, TimeSpan window, CancellationToken ct = default);
}