namespace DigiTekShop.Contracts.Abstractions.Identity.Device;
    public interface IDeviceRegistry
    {
        Task UpsertAsync(Guid userId, string deviceId, string? ua, string? ip, CancellationToken ct);
        Task<bool> IsTrustedAsync(Guid userId, string deviceId, CancellationToken ct);
        Task<DateTimeOffset?> TrustAsync(Guid userId, string deviceId, TimeSpan window, CancellationToken ct);

    }
