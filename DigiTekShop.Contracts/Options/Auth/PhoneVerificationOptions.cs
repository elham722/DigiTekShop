namespace DigiTekShop.Contracts.Options.Auth;

public sealed class PhoneVerificationOptions
{
    public int CodeLength { get; init; } = 6;
    public int CodeValidityMinutes { get; init; } = 5;
    public int MaxAttempts { get; init; } = 5;
    public bool RequirePhoneConfirmation { get; init; } = true;
    public bool AllowResendCode { get; init; } = true;
    public int ResendCooldownMinutes { get; init; } = 2;

    public PhoneSecurityOptions Security { get; init; } = new();
    public PhoneTemplateOptions Template { get; init; } = new();
}

public sealed class PhoneSecurityOptions
{
    public string AllowedPhonePattern { get; init; } = @"^\+98[0-9]{10}$";
    public int MaxRequestsPerHour { get; init; } = 5;
    public int MaxRequestsPerDay { get; init; } = 20;
    public int MaxRequestsPerMonth { get; init; } = 100;
    public bool RequireUniquePhoneNumbers { get; init; } = true;
    public string CountryCode { get; init; } = "+98";
    public bool IpRestrictionEnabled { get; init; }
    public int MaxRequestsPerIpPerHour { get; init; } = 10;
}

public sealed class PhoneTemplateOptions
{
    public string CompanyName { get; init; } = "DigiTekShop";
    public string SupportPhoneNumber { get; init; } = "";
    public string WebsiteUrl { get; init; } = "https://digitekshop.com";
    public string CodeTemplate { get; init; } = "{1} - کد تأیید شما: {0}";
    public string CodeTemplateEnglish { get; init; } = "{1} - Your verification code: {0}";
    public bool UseEnglishTemplate { get; init; }
    public int MaxSmsLength { get; init; } = 160;
    public string SenderNumber { get; init; } = "";
}