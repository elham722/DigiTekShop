namespace DigiTekShop.Contracts.DTOs.PhoneVerification;

public sealed class PhoneTemplateSettings
{
    public string CompanyName { get; set; } = "DigiTekShop";

    public string SupportPhoneNumber { get; set; } = string.Empty;

    public string WebsiteUrl { get; set; } = string.Empty;

    public string CodeTemplate { get; set; } = "{1} - کد تأیید شما: {0}";

    public string CodeTemplateEnglish { get; set; } = "{1} - Your verification code: {0}";

    public bool UseEnglishTemplate { get; set; } = false;

    public int MaxSmsLength { get; set; } = 160;

    public string SenderNumber { get; set; } = string.Empty;
}