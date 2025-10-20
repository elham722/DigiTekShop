using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.Options.Security;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Services.Device;

public sealed class DeviceRegistry : IDeviceRegistry
{
    private static class Events
    {
        public static readonly EventId Upsert = new(20001, nameof(UpsertAsync));
        public static readonly EventId Trust = new(20002, nameof(TrustAsync));
        public static readonly EventId Check = new(20003, nameof(IsTrustedAsync));
    }

    private readonly DigiTekShopIdentityDbContext _db;
    private readonly IDateTimeProvider _time;
    private readonly DeviceLimitsOptions _limits;
    private readonly ILogger<DeviceRegistry> _log;
    private readonly IDistributedLockService? _lock; 

    public DeviceRegistry(
        DigiTekShopIdentityDbContext db,
        IDateTimeProvider time,
        IOptions<DeviceLimitsOptions> limits,
        ILogger<DeviceRegistry> log,
        IDistributedLockService? @lock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _limits = (limits?.Value) ?? new DeviceLimitsOptions();
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _lock = @lock;
    }

    public async Task UpsertAsync(Guid userId, string deviceId, string? ua, string? ip, CancellationToken ct)
    {
        if (userId == Guid.Empty) return;
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        var nowUtc = _time.UtcNow;
        var devId = Normalization.DeviceId(deviceId);
        var uaClean = Normalization.UserAgent(ua);
        var ipClean = Normalization.Ip(ip);

        var entity = await _db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == devId, ct);

        if (entity is null)
        {
            try
            {
                entity = UserDevice.Create(userId, devId, devId, nowUtc, ipClean, null, uaClean, null);
                _db.UserDevices.Add(entity);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _log.LogWarning(Events.Upsert, ex, "Race on user-device insert, retrying fetch. user={UserId}, device={DeviceId}", userId, devId);
                entity = await _db.UserDevices.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == devId, ct);
                if (entity is null) throw; 
            }
        }

       
        if (entity is not null)
        {
            entity.Touch(nowUtc, ipClean, uaClean, null);

            if (_limits.AutoDeactivateInactiveDevices && _limits.DeviceInactivityThreshold > TimeSpan.Zero)
            {
                var cutoff = nowUtc - _limits.DeviceInactivityThreshold;
                var inactive = await _db.UserDevices
                    .Where(d => d.UserId == userId && d.LastSeenUtc < cutoff)
                    .ToListAsync(ct);

                foreach (var d in inactive)
                    d.Untrust(); 

            }

            await _db.SaveChangesAsync(ct);
        }

        _log.LogDebug(Events.Upsert, "Upserted device. user={UserId}, device={DeviceId}", userId, devId);
    }

    public async Task<bool> IsTrustedAsync(Guid userId, string deviceId, CancellationToken ct)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(deviceId))
            return false;

        var now = _time.UtcNow;
        var devId = Normalization.DeviceId(deviceId);

        var ok = await _db.UserDevices
            .AnyAsync(d => d.UserId == userId
                        && d.DeviceId == devId
                        && d.TrustedUntilUtc != null
                        && d.TrustedUntilUtc >= now, ct);

        _log.LogDebug(Events.Check, "Check device trust. user={UserId}, device={DeviceId}, trusted={Trusted}", userId, devId, ok);
        return ok;
    }

    public async Task<DateTimeOffset?> TrustAsync(Guid userId, string deviceId, TimeSpan window, CancellationToken ct)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(deviceId))
            return null;

        var devId = Normalization.DeviceId(deviceId);
        var nowUtc = _time.UtcNow;

        var lockKey = $"device-trust:{userId}";
        string? lockToken = null;
        if (_lock is not null)
            lockToken = await _lock.AcquireAsync(lockKey, TimeSpan.FromSeconds(5), ct);

        try
        {
            var device = await _db.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == devId, ct);

            if (device is null)
            {
                device = UserDevice.Create(userId, devId, devId, nowUtc);
                _db.UserDevices.Add(device);
            }

            if (_limits.MaxTrustedDevicesPerUser > 0)
            {
                var trusted = await _db.UserDevices
                    .Where(d => d.UserId == userId && d.TrustedUntilUtc != null && d.TrustedUntilUtc >= nowUtc)
                    .OrderBy(d => d.TrustedUntilUtc) 
                    .ToListAsync(ct);

                var alreadyTrusted = trusted.Any(d => d.DeviceId == devId);

                if (!alreadyTrusted && trusted.Count >= _limits.MaxTrustedDevicesPerUser)
                {
                    var victim = trusted.First();
                    victim.Untrust();
                    _log.LogInformation(Events.Trust, "Evicted trusted device to honor MaxTrustedDevicesPerUser. user={UserId}, victim={VictimDeviceId}", userId, victim.DeviceId);
                }
            }

            
            var desired = nowUtc + window;
            var maxSpan = _limits.DeviceTokenExpiration > TimeSpan.Zero ? _limits.DeviceTokenExpiration : TimeSpan.FromDays(90);
            var maxToken = nowUtc + maxSpan;
            var until = desired <= maxToken ? desired : maxToken;

            if (device.TrustedUntilUtc is { } exists && exists > nowUtc)
                device.TrustUntil(exists > until ? exists : until);  
            else
                device.TrustUntil(until);

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(Events.Trust, "Trusted device. user={UserId}, device={DeviceId}, until={UntilUtc:o}", userId, devId, device.TrustedUntilUtc);
            return device.TrustedUntilUtc;
        }
        finally
        {
            if (lockToken is not null && _lock is not null)
                await _lock.ReleaseAsync(lockKey,lockToken ,ct);
        }
    }
}
