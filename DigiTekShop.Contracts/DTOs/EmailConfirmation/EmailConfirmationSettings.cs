namespace DigiTekShop.Contracts.DTOs.EmailConfirmation;

public sealed class EmailConfirmationSettings
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ConfirmEmailPath { get; set; } = "account/confirm-email";

    public int TokenValidityMinutes { get; set; } = 60 * 24; // 24 ساعت

    public bool RequireEmailConfirmation { get; set; } = true;

    public bool AllowResendConfirmation { get; set; } = true;

    public int ResendCooldownMinutes { get; set; } = 5;

    public EmailTemplateSettings Template { get; set; } = new();
}

