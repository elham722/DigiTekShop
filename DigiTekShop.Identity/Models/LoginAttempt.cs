using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Identity.Models;

public class LoginAttempt
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public string? LoginNameOrEmail { get; private set; }
    public DateTime AttemptedAt { get; private set; }
    public LoginStatus Status { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? LoginNameOrEmailNormalized { get; private set; }

    private LoginAttempt()
    {
    }

    public static LoginAttempt Create(
        Guid? userId,
        LoginStatus status,
        DateTime attemptedAtUtc,             
        string? ipAddress = null,
        string? userAgent = null,
        string? loginNameOrEmail = null,
        string? loginNameOrEmailNormalized = null)
    {
        return new LoginAttempt
        {
            UserId = userId,
            Status = status,
            AttemptedAt = DateTime.SpecifyKind(attemptedAtUtc, DateTimeKind.Utc),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            LoginNameOrEmail = string.IsNullOrWhiteSpace(loginNameOrEmail) ? null : loginNameOrEmail,
            LoginNameOrEmailNormalized = string.IsNullOrWhiteSpace(loginNameOrEmailNormalized)
                ? Normalize(loginNameOrEmail)
                : loginNameOrEmailNormalized
        };
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
}


