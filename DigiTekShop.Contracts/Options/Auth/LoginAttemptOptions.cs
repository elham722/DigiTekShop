namespace DigiTekShop.Contracts.Options.Auth;

public sealed class LoginAttemptOptions
{
    public int MaxListLimit { get; init; } = 200;     
    public bool MaskSensitiveInLogs { get; init; } = true;
    public string IpMaskReplacement { get; init; } = "*";
}