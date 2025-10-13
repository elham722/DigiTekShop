
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Exceptions.NotFound;
using DigiTekShop.SharedKernel.Exceptions.Validation;

namespace DigiTekShop.Identity.Models;
public class User : IdentityUser<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string? GoogleId { get; private set; }
    public string? MicrosoftId { get; private set; }

    public bool TermsAccepted { get; private set; } = true;

    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public ICollection<UserDevice> Devices { get; private set; } = new List<UserDevice>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; private set; } = new List<PasswordResetToken>();
    public ICollection<PasswordHistory> PasswordHistories { get; private set; } = new List<PasswordHistory>();
    
    // MFA relationship
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
        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    public void UpdatePasswordChange(DateTime date)
    {
        LastPasswordChangeAt = date;
        Touch();
    }

    public void RecordLogin(DateTime date)
    {
        LastLoginAt = date;
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

    // Devices
    public void AddDevice(UserDevice device, int maxActiveDevices = 5, int maxTrustedDevices = 3)
    {
        Guard.AgainstNull(device, nameof(device));
        
        if (Devices.Any(d => d.Id == device.Id)) 
            return; 
        
      
        var activeDevices = Devices.Count(d => d.IsActive);
        if (activeDevices >= maxActiveDevices)
        {
            throw new InvalidDomainOperationException(
                $"Maximum active devices limit ({maxActiveDevices}) exceeded",
                ErrorCodes.Identity.MAX_ACTIVE_DEVICES_EXCEEDED);
        }
        
       
        if (device.IsTrusted)
        {
            var trustedDevices = Devices.Count(d => d.IsTrusted);
            if (trustedDevices >= maxTrustedDevices)
            {
                throw new InvalidDomainOperationException(
                    $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                    ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);
            }
        }
        
        Devices.Add(device);
        Touch();
    }

    public void RemoveDevice(Guid deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device != null)
        {
            Devices.Remove(device);
            Touch();
        }
    }

   
    public void DeactivateInactiveDevices(TimeSpan inactivityThreshold)
    {
        var cutoffDate = DateTime.UtcNow - inactivityThreshold;
        var inactiveDevices = Devices.Where(d => d.IsActive && d.LastLoginAt < cutoffDate).ToList();
        
        foreach (var device in inactiveDevices)
        {
            device.Deactivate();
        }
        
        if (inactiveDevices.Any())
            Touch();
    }

   
    public void RemoveOldInactiveDevices(TimeSpan removalThreshold)
    {
        var cutoffDate = DateTime.UtcNow - removalThreshold;
        var oldDevices = Devices.Where(d => !d.IsActive && d.LastLoginAt < cutoffDate).ToList();
        
        foreach (var device in oldDevices)
        {
            Devices.Remove(device);
        }
        
        if (oldDevices.Any())
            Touch();
    }

 
    public void TrustDevice(Guid deviceId, int maxTrustedDevices = 3)
    {
        var device = Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
            throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        if (!device.IsActive)
            throw new InvalidDomainOperationException("Cannot trust inactive device", ErrorCodes.Identity.DEVICE_INACTIVE);

       
        var trustedDevices = Devices.Count(d => d.IsTrusted);
        if (trustedDevices >= maxTrustedDevices)
        {
            throw new InvalidDomainOperationException(
                $"Maximum trusted devices limit ({maxTrustedDevices}) exceeded",
                ErrorCodes.Identity.MAX_TRUSTED_DEVICES_EXCEEDED);
        }

        device.MarkAsTrusted();
        Touch();
    }

  
    public void UntrustDevice(Guid deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
            throw new NotFoundException("Device not found", ErrorCodes.Identity.DEVICE_NOT_FOUND);

        device.MarkAsUntrusted();
        Touch();
    }

   
    public int GetActiveDeviceCount() => Devices.Count(d => d.IsActive);

  
    public int GetTrustedDeviceCount() => Devices.Count(d => d.IsTrusted);

   
    public UserDevice? GetDeviceByFingerprint(string fingerprint)
    {
        return Devices.FirstOrDefault(d => d.DeviceFingerprint == fingerprint);
    }


    public bool HasDeviceWithFingerprint(string fingerprint)
    {
        return Devices.Any(d => d.DeviceFingerprint == fingerprint);
    }

    // RefreshTokens
    public void AddRefreshToken(RefreshToken token)
    {
        Guard.AgainstNull(token, nameof(token));

        if (RefreshTokens.Any(t => t.TokenHash == token.TokenHash)) return; 
        RefreshTokens.Add(token);
        Touch();
    }

    public void RevokeRefreshToken(string tokenHash)
    {
        var refreshToken = RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.Revoke();
            Touch();
        }
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in RefreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
        Touch();
    }

    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    public bool IsActive => !IsLocked && EmailConfirmed && !IsDeleted;
    public bool CanLogin() => IsActive && !IsLocked;

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
