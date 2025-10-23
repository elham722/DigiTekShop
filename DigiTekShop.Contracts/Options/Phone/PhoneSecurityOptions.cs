namespace DigiTekShop.Contracts.Options.Phone;
public sealed class PhoneSecurityOptions
{
    public string AllowedPhonePattern { get; set; } = "^\\+[1-9]\\d{1,14}$";

    public int MaxRequestsPerHour { get; set; } = 5;
    public int MaxRequestsPerDay { get; set; } = 20;
    public int MaxRequestsPerMonth { get; set; } = 100;

    public bool RequireUniquePhoneNumbers { get; set; } = true;
    public string? CountryCode { get; set; } = "+98"; 

    public bool IpRestrictionEnabled { get; set; } = false;
    public int MaxRequestsPerIpPerHour { get; set; } = 10;
}
