namespace DigiTekShop.Contracts.Auth.ResetPassword;

public record PasswordResetThrottleStatus(
    bool HasActiveToken,
    bool IsThrottled,
    int AttemptCount,
    DateTime? ThrottleUntil,
    DateTime? LastAttemptAt
)
{
    public TimeSpan? RemainingThrottleTime =>
        ThrottleUntil.HasValue && ThrottleUntil.Value > DateTime.UtcNow
            ? ThrottleUntil.Value - DateTime.UtcNow
            : null;
}
