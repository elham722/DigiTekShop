namespace DigiTekShop.Contracts.Abstractions.Caching;
public interface IDistributedLockService
{
    Task<string?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);
    Task ReleaseAsync(string key, string lockToken, CancellationToken ct = default);
}
