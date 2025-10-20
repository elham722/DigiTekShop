namespace DigiTekShop.Contracts.Options.Auth;

public sealed class PhoneVerificationOptions
{
    public int CodeLength { get; set; } = 6;
    public TimeSpan CodeValidity { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; set; } = 5;
    public bool RequirePhoneConfirmation { get; set; } = true;
    public bool AllowResendCode { get; set; } = true;
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromMinutes(2);

    public PhoneSecurityOptions Security { get; init; } = new();
    public PhoneTemplateOptions Template { get; init; } = new();
}

public sealed class PhoneSecurityOptions
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

public sealed class PhoneTemplateOptions
{
    public string CompanyName { get; set; } = "DigiTekShop";

    public string SupportPhoneNumber { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public string CodeTemplate { get; set; } = "{1} - کد تأیید شما: {0}";

    public string CodeTemplateEnglish { get; set; } = "{1} - Your verification code: {0}";

    public bool UseEnglishTemplate { get; set; } = false;

    public int MaxSmsLength { get; set; } = 160;

    public string SenderNumber { get; set; } = string.Empty;

    public string? OtpTemplateName { get; set; } = "login-otp";
}