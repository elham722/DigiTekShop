namespace DigiTekShop.Contracts.Auth.Mfa
{
    public record MfaStatusDto(
        bool IsEnabled,
        bool IsLocked = false,
        int AttemptCount = 0,
        DateTimeOffset? LockedUntil = null,
        DateTimeOffset? LastVerifiedAt = null
    );
}
