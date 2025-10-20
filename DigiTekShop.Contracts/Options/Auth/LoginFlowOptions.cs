namespace DigiTekShop.Contracts.Options.Auth;
public sealed class LoginFlowOptions
{
    public bool EnableMfa { get; init; } = false;

    public int MfaChallengeTtlSeconds { get; init; } = 120;

    public int TrustDeviceDays { get; init; } = 30;

    public RateLimitOptions RateLimit { get; init; } = new();
}
