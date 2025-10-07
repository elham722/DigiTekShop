namespace DigiTekShop.Identity.Options.PhoneVerification;

public sealed class PhoneSecuritySettings
{
    public string AllowedPhonePattern { get; set; } = "^\\+[1-9]\\d{1,14}$";

    public int MaxRequestsPerHour { get; set; } = 5;

    public int MaxRequestsPerDay { get; set; } = 20;

    public int MaxRequestsPerMonth { get; set; } = 100;

    public bool RequireUniquePhoneNumbers { get; set; } = true;

    public string? CountryCode { get; set; } = null;

    public bool IpRestrictionEnabled { get; set; } = false;

    public int MaxRequestsPerIpPerHour { get; set; } = 10;
}