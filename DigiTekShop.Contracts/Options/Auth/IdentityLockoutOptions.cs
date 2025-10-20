namespace DigiTekShop.Contracts.Options.Auth;
public sealed class IdentityLockoutOptions
{
    public TimeSpan DefaultDuration { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromDays(7);
    public bool AllowManualUnlock { get; init; } = true;
}
