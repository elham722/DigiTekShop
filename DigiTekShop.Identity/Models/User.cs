using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.NotFound;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using Microsoft.AspNetCore.Identity;

namespace DigiTekShop.Identity.Models;

public class User : IdentityUser<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string? GoogleId { get; private set; }
    public string? MicrosoftId { get; private set; }

    public bool TermsAccepted { get; private set; } = true;

    public bool IsDeleted { get; private set; }


    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime? LastPasswordChangeAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    public ICollection<UserDevice> Devices { get; private set; } = new List<UserDevice>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; private set; } = new List<PasswordResetToken>();
    public ICollection<PasswordHistory> PasswordHistories { get; private set; } = new List<PasswordHistory>();

    public UserMfa? Mfa { get; private set; }

    private User() : base() { }

    public static User Create(string email, string userName, Guid? customerId = null)
    {
        Guard.AgainstNullOrEmpty(email, nameof(email));
        Guard.AgainstNullOrEmpty(userName, nameof(userName));

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            UserName = userName,
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
        DeletedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void UpdatePasswordChange(DateTime whenUtc)
    {
        LastPasswordChangeAtUtc = EnsureUtc(whenUtc);
        Touch();
    }

    public void RecordLogin(DateTime whenUtc)
    {
        LastLoginAtUtc = EnsureUtc(whenUtc);
        Touch();
    }

    public void SetGoogleId(string? googleId)
    {
        GoogleId = string.IsNullOrWhiteSpace(googleId) ? null : googleId;
        Touch();
    }

    public void SetMicrosoftId(string? microsoftId)
    {
        MicrosoftId = string.IsNullOrWhiteSpace(microsoftId) ? null : microsoftId;
        Touch();
    }

   
    public void AddDevice(
        UserDevice device,
        DateTime nowUtc,
        int maxActiveDevices = 5,
        int maxTrustedDevices = 3)
    {
        Guard.AgainstNull(device, nameof(device));
        nowUtc = EnsureUtc(nowUtc);

        if (Devices.Any(d => d.Id == device.Id))
            return; 

        var activeCount = Devices.Count(d => d.IsActive);
        if (activeCount >= maxActiveDevices)
        {
            throw new InvalidDomainOperationException(
                $"Maximum active devices limit ({maxActiveDevices}) exceeded",
                ErrorCodes.Identity.MAX_ACTIVE_DEVICES_EXCEEDED);
        }

       
        if (device.IsCurrentlyTrusted(nowUtc))
        {
            var trustedCount = Devices.Count(d => d.IsCurrentlyTrusted(nowUtc));
            if (trustedCount >= maxTrustedDevices)
            {
                throw new InvalidDomainOperationException(
                    $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                    ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);
            }
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

    public void DeactivateInactiveDevices(TimeSpan inactivityThreshold, DateTime nowUtc)
    {
        nowUtc = EnsureUtc(nowUtc);
        var cutoff = nowUtc - inactivityThreshold;
        var toDeactivate = Devices.Where(d => d.IsActive && d.LastSeenUtc < cutoff).ToList();

        foreach (var d in toDeactivate)
            d.Deactivate();

        if (toDeactivate.Any())
            Touch();
    }

    public void RemoveOldInactiveDevices(TimeSpan removalThreshold, DateTime nowUtc)
    {
        nowUtc = EnsureUtc(nowUtc);
        var cutoff = nowUtc - removalThreshold;
        var olds = Devices.Where(d => !d.IsActive && d.LastSeenUtc < cutoff).ToList();

        foreach (var d in olds)
            Devices.Remove(d);

        if (olds.Any())
            Touch();
    }

  
    public void TrustDevice(Guid entityDeviceId, TimeSpan window, DateTime nowUtc, int maxTrustedDevices = 3)
    {
        nowUtc = EnsureUtc(nowUtc);
        var device = Devices.FirstOrDefault(d => d.Id == entityDeviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        if (!device.IsActive)
            throw new InvalidDomainOperationException("Cannot trust inactive device", ErrorCodes.Identity.DEVICE_INACTIVE);

        var trustedCount = Devices.Count(d => d.IsCurrentlyTrusted(nowUtc));
        if (!device.IsCurrentlyTrusted(nowUtc) && trustedCount >= maxTrustedDevices)
        {
            throw new InvalidDomainOperationException(
                $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);
        }

        device.TrustFor(window, nowUtc);
        Touch();
    }

   
    public void TrustDeviceByDeviceId(string deviceId, TimeSpan window, DateTime nowUtc, int maxTrustedDevices = 3)
        => TrustDevice(FindDeviceEntityIdByDeviceId(deviceId), window, nowUtc, maxTrustedDevices);

    public void UntrustDevice(Guid entityDeviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == entityDeviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        device.Untrust();
        Touch();
    }

    public void UntrustDeviceByDeviceId(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        device.Untrust();
        Touch();
    }

    public int GetActiveDeviceCount() => Devices.Count(d => d.IsActive);

    public int GetTrustedDeviceCount(DateTime nowUtc)
    {
        nowUtc = EnsureUtc(nowUtc);
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
        foreach (var t in RefreshTokens.Where(t => t.IsActive))
            t.Revoke();
        Touch();
    }


    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public bool IsActiveUser => !IsLocked && EmailConfirmed && !IsDeleted;
    public bool CanLogin() => IsActiveUser && !IsLocked;

   

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static DateTime EnsureUtc(DateTime dt)
        => dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);

    private Guid FindDeviceEntityIdByDeviceId(string deviceId)
    {
        var dev = Devices.FirstOrDefault(d => d.DeviceId == deviceId)
            ?? throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);
        return dev.Id;
    }
}
