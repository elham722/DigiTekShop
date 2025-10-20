namespace DigiTekShop.Contracts.Options.Auth;
public sealed class RateLimitOptions
{
    public int Limit { get; init; } = 8;

    public int WindowSeconds { get; init; } = 60;
}