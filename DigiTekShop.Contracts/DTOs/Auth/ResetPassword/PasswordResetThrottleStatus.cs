namespace DigiTekShop.Contracts.DTOs.Auth.ResetPassword;

public class PasswordResetThrottleStatus
{
    public bool HasActiveToken { get; set; }

    public bool IsThrottled { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? ThrottleUntil { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public TimeSpan? RemainingThrottleTime =>
        ThrottleUntil.HasValue && ThrottleUntil.Value > DateTime.UtcNow
            ? ThrottleUntil.Value - DateTime.UtcNow
            : null;
}
