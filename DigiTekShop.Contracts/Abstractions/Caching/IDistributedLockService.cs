namespace DigiTekShop.Contracts.Abstractions.Caching
{
    public interface IDistributedLockService
    {
        Task<bool> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);
        Task ReleaseAsync(string key, CancellationToken ct = default);
    }
}
