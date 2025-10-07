namespace DigiTekShop.Identity.Options;

public class DeviceLimitsSettings
{
    public int MaxActiveDevicesPerUser { get; init; } = 5;

    public int MaxTrustedDevicesPerUser { get; init; } = 3;

    public TimeSpan DeviceInactivityThreshold { get; init; } = TimeSpan.FromDays(30);

    public int MaxTrustAttempts { get; init; } = 3;

    public TimeSpan DeviceTokenExpiration { get; init; } = TimeSpan.FromDays(90);

    public bool AutoDeactivateInactiveDevices { get; init; } = true;

    public bool DefaultTrustNewDevices { get; init; } = false;
}
