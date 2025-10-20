namespace DigiTekShop.Contracts.Options.Security;

public sealed class SecurityEventsOptions
{
    public int MaxListLimit { get; init; } = 200;
    public int TopIpCount { get; init; } = 10;

    public int MaxMetadataLength { get; init; } = 8192;
    public int MaxUserAgentLength { get; init; } = 1024;
    public int MaxDeviceIdLength { get; init; } = 128;
    public int MaxIpLength { get; init; } = 64;

    public Dictionary<string, string> DefaultSeverityMappings { get; init; } = new()
    {
        ["FailedLogin"] = "Medium",
        ["MfaFailed"] = "High",
        ["PasswordChanged"] = "Low",
        ["RefreshTokenAbuse"] = "High"
    };
}