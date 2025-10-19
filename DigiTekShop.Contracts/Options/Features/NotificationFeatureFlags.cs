namespace DigiTekShop.Contracts.Options.Features;

public sealed class NotificationFeatureFlags
{
    public const string SectionName = "FeatureFlags:Notifications";

    public bool EnableEmailOnRegistration { get; set; } = true;
    public bool EnableSmsOnRegistration { get; set; } = true;
}

