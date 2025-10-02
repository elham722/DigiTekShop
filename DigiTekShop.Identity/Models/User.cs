namespace DigiTekShop.Identity.Models;
public class User : IdentityUser<Guid>
{
    public Guid? CustomerId { get; private set; }
    public string? GoogleId { get; private set; }
    public string? MicrosoftId { get; private set; }

    public string? TotpSecretKey { get; private set; }
    public bool TotpEnabled { get; private set; }

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
            LockoutEnabled = true
        };
    }

    public void SetCustomerId(Guid? customerId)
    {
        CustomerId = customerId;
        Touch();
    }

    public void SetTotpSecretKey(string? secretKey)
    {
        TotpSecretKey = secretKey;
        Touch();
    }

    public void SetTotpEnabled(bool enabled)
    {
        TotpEnabled = enabled;
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
    public void AddDevice(UserDevice device)
    {
       Guard.AgainstNull(device,nameof(device));
        if (Devices.Any(d => d.Id == device.Id)) return; 
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
