namespace DigiTekShop.Contracts.DTOs.Auth.LoginAttempt;

public record LoginAttemptDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? LoginNameOrEmail { get; init; }
    public DateTime AttemptedAt { get; init; }
    public LoginStatus Status { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string StatusDisplayName => Status.ToString();
    public string? MaskedIpAddress => !string.IsNullOrEmpty(IpAddress) 
        ? MaskIpAddress(IpAddress) 
        : null;
    public string? MaskedUserAgent => !string.IsNullOrEmpty(UserAgent) 
        ? MaskUserAgent(UserAgent) 
        : null;

    private static string MaskIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return ipAddress;

        var parts = ipAddress.Split('.');
        if (parts.Length == 4) // IPv4
        {
            return $"{parts[0]}.{parts[1]}.***.***";
        }

        // IPv6 or other formats - mask last 3 characters
        return ipAddress.Length > 3 
            ? ipAddress[..^3] + "***" 
            : "***";
    }

    private static string MaskUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return userAgent;

        // Keep first 50 characters and mask the rest
        return userAgent.Length > 50 
            ? userAgent[..50] + "..." 
            : userAgent;
    }
}
