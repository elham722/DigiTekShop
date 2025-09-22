using System;

namespace DigiTekShop.Identity.Models;
public class User : IdentityUser
{
    public string? CustomerId { get; private set; }

    public string? GoogleId { get; private set; }
    public string? MicrosoftId { get; private set; }

    // MFA (TOTP)
    public string? TotpSecretKey { get; private set; }
    public bool TotpEnabled { get; private set; }

    // States
    public bool IsDeleted { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // روابط
    public ICollection<UserDevice> Devices { get; private set; } = new List<UserDevice>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() : base() { } // EF Core

    public static User Create(string email, string userName, string? customerId = null)
    {
       // Guard.AgainstNullOrEmpty(email, nameof(email));
      //  Guard.AgainstNullOrEmpty(userName, nameof(userName));

        return new User()
        {
            Email = email,
            UserName = userName,
            CustomerId = customerId,
            LockoutEnabled = true
        };
    }

    // Custom setters
    internal void SetCustomerId(string? customerId) => CustomerId = customerId;
    internal void SetTotpSecretKey(string? secretKey) => TotpSecretKey = secretKey;
    internal void SetTotpEnabled(bool enabled) => TotpEnabled = enabled;
    internal void MarkDeleted() => IsDeleted = true;
    internal void UpdatePasswordChange(DateTime date) => LastPasswordChangeAt = date;
    internal void UpdateLastLogin(DateTime date) => LastLoginAt = date;
    internal void SetGoogleId(string? googleId) => GoogleId = googleId;
    internal void SetMicrosoftId(string? microsoftId) => MicrosoftId = microsoftId;

    // Computed
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    public bool IsActive => !IsLocked && EmailConfirmed && !IsDeleted;

    // Business
    public bool CanLogin() => IsActive && !IsLocked;
}
