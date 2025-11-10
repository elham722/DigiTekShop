using DigiTekShop.SharedKernel.Exceptions.NotFound;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public class User : IdentityUser<Guid>
{
    public Guid? CustomerId { get; private set; }

    public bool TermsAccepted { get; private set; } = true;
    public bool IsDeleted { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } // Set by DB via SYSUTCDATETIME()
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public string? NormalizedPhoneNumber { get; private set; }

    public ICollection<UserDevice> Devices { get; private set; } = new List<UserDevice>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();

    public UserMfa? Mfa { get; private set; } 

    private User() : base() { }

    public static User CreateFromPhone(string rawPhone, Guid? customerId = null, bool phoneConfirmed = false)
    {
        var phone = Normalization.NormalizePhoneIranE164(rawPhone);

        return new User
        {
            UserName = phone,                    
            PhoneNumber = phone,
            NormalizedPhoneNumber = phone,       
            PhoneNumberConfirmed = phoneConfirmed,
            CustomerId = customerId,
            LockoutEnabled = true,
            TermsAccepted = true
        };
    }

    public void SetCustomerId(Guid? customerId)
    {
        CustomerId = customerId;
        Touch();
    }

    public void Deactivate()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RecordLogin(DateTimeOffset whenUtc)
    {
        LastLoginAtUtc = whenUtc;
        Touch();
    }

    public void SetPhoneNumber(string rawPhone, bool confirmed = false)
    {
        var phone = Normalization.NormalizePhoneIranE164(rawPhone);
        PhoneNumber = phone;
        NormalizedPhoneNumber = phone;
        PhoneNumberConfirmed = confirmed;
        Touch();
    }

    public void MarkPhoneConfirmed()
    {
        if (!PhoneNumberConfirmed)
        {
            PhoneNumberConfirmed = true;
            Touch();
        }
    }

  

    public void AddDevice(UserDevice device, DateTimeOffset nowUtc, int maxActiveDevices = 5, int maxTrustedDevices = 3)
    {
        Guard.AgainstNull(device, nameof(device));

        // Check for duplicate by Id (entity-level)
        if (Devices.Any(d => d.Id == device.Id)) return;

        // Check for duplicate by DeviceId (database unique constraint)
        // This prevents DB unique constraint violation before SaveChanges
        if (Devices.Any(d => d.DeviceId == device.DeviceId)) return;

        var activeCount = Devices.Count(d => d.IsActive);
        if (activeCount >= maxActiveDevices)
            throw new InvalidDomainOperationException(
                $"Maximum active devices limit ({maxActiveDevices}) exceeded",
                ErrorCodes.Identity.MAX_ACTIVE_DEVICES_EXCEEDED);

        if (device.IsCurrentlyTrusted(nowUtc))
        {
            var trustedCount = Devices.Count(d => d.IsCurrentlyTrusted(nowUtc));
            if (trustedCount >= maxTrustedDevices)
                throw new InvalidDomainOperationException(
                    $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                    ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);
        }

        Devices.Add(device);
        Touch();
    }

    public void RemoveDevice(Guid entityDeviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == entityDeviceId);
        if (device is null) return;
        Devices.Remove(device);
        Touch();
    }

    public void RemoveDeviceByDeviceId(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device is null) return;
        Devices.Remove(device);
        Touch();
    }

    public void DeactivateInactiveDevices(TimeSpan inactivityThreshold, DateTimeOffset nowUtc)
    {
        var cutoff = nowUtc - inactivityThreshold;
        var toDeactivate = Devices.Where(d => d.IsActive && d.LastSeenUtc < cutoff).ToList();
        foreach (var d in toDeactivate) d.Deactivate();
        if (toDeactivate.Any()) Touch();
    }

    public void RemoveOldInactiveDevices(TimeSpan removalThreshold, DateTimeOffset nowUtc)
    {
        var cutoff = nowUtc - removalThreshold;
        var olds = Devices.Where(d => !d.IsActive && d.LastSeenUtc < cutoff).ToList();
        foreach (var d in olds) Devices.Remove(d);
        if (olds.Any()) Touch();
    }

    public void TrustDevice(Guid entityDeviceId, TimeSpan window, DateTimeOffset nowUtc, int maxTrustedDevices = 3)
    {
       
        var device = Devices.FirstOrDefault(d => d.Id == entityDeviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        if (!device.IsActive)
            throw new InvalidDomainOperationException("Cannot trust inactive device", ErrorCodes.Identity.DEVICE_INACTIVE);

        var trustedCount = Devices.Count(d => d.IsCurrentlyTrusted(nowUtc));
        if (!device.IsCurrentlyTrusted(nowUtc) && trustedCount >= maxTrustedDevices)
            throw new InvalidDomainOperationException(
                $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);

        device.TrustFor(window, nowUtc);
        Touch();
    }

    public void UntrustDevice(Guid entityDeviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == entityDeviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);
        device.Untrust();
        Touch();
    }

    public int GetActiveDeviceCount() => Devices.Count(d => d.IsActive);

    public int GetTrustedDeviceCount(DateTimeOffset nowUtc)
    {
        return Devices.Count(d => d.IsCurrentlyTrusted(nowUtc));
    }

    public UserDevice? GetDeviceByFingerprint(string fingerprint)
        => Devices.FirstOrDefault(d => d.DeviceFingerprint == fingerprint);

    public bool HasDeviceWithFingerprint(string fingerprint)
        => Devices.Any(d => d.DeviceFingerprint == fingerprint);

    public void AddRefreshToken(RefreshToken token)
    {
        Guard.AgainstNull(token, nameof(token));
        if (RefreshTokens.Any(t => t.TokenHash == token.TokenHash)) return;
        RefreshTokens.Add(token);
        Touch();
    }

    public void RevokeRefreshToken(string tokenHash)
    {
        var t = RefreshTokens.FirstOrDefault(x => x.TokenHash == tokenHash);
        if (t is not null && t.IsActive)
        {
            t.Revoke();
            Touch();
        }
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var t in RefreshTokens.Where(t => t.IsActive)) t.Revoke();
        Touch();
    }

    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public bool IsActiveUser => !IsLocked && PhoneNumberConfirmed && !IsDeleted;
    public bool CanLogin() => IsActiveUser && !IsLocked;

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
