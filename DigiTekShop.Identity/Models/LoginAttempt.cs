using DigiTekShop.SharedKernel.Enums.Auth;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public sealed class LoginAttempt
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? UserId { get; private set; }
    public string? LoginNameOrEmail { get; private set; }
    public string? LoginNameOrEmailNormalized { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
    public LoginStatus Status { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }

    // Correlation fields for request tracking
    public string? CorrelationId { get; private set; }
    public string? RequestId { get; private set; }

    private LoginAttempt() { }

    public static LoginAttempt Create(
        Guid? userId,
        LoginStatus status,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceId = null,
        string? loginNameOrEmail = null,
        string? correlationId = null,
        string? requestId = null)
    {
        // Normalize login name/email
        string? normalized = null;
        string? trimmedLogin = null;

        if (!string.IsNullOrWhiteSpace(loginNameOrEmail))
        {
            trimmedLogin = loginNameOrEmail.Trim();
            
            // Try phone normalization first (E.164)
            if (Normalization.TryNormalizePhoneIranE164(trimmedLogin, out var e164) && e164 is not null)
            {
                normalized = e164;
            }
            else
            {
                // Email normalization: lowercase and trim
                normalized = trimmedLogin.ToLowerInvariant();
            }
        }

        // Normalize and truncate string fields
        var normalizedIp = StringNormalizer.NormalizeAndTruncate(ipAddress, 45);
        var normalizedUserAgent = StringNormalizer.NormalizeAndTruncate(userAgent, 1024);
        var normalizedDeviceId = StringNormalizer.NormalizeAndTruncate(deviceId, 128);
        var normalizedCorrelationId = StringNormalizer.NormalizeAndTruncate(correlationId, 128);
        var normalizedRequestId = StringNormalizer.NormalizeAndTruncate(requestId, 128);
        var normalizedLoginName = StringNormalizer.NormalizeAndTruncate(trimmedLogin, 256);

        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            // AttemptedAt will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            IpAddress = normalizedIp,
            UserAgent = normalizedUserAgent,
            DeviceId = normalizedDeviceId,
            LoginNameOrEmail = normalizedLoginName,
            LoginNameOrEmailNormalized = normalized,
            CorrelationId = normalizedCorrelationId,
            RequestId = normalizedRequestId
        };
    }

}


