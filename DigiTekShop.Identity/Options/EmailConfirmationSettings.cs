namespace DigiTekShop.Identity.Options;

public sealed class EmailConfirmationSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ConfirmEmailPath { get; set; } = "account/confirm-email";
    public TimeSpan TokenValidity { get; set; } = TimeSpan.FromHours(24);
    public bool RequireEmailConfirmation { get; set; } = true;
    public bool AllowResendConfirmation { get; set; } = true;
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromMinutes(5);
    public EmailTemplateSettings Template { get; set; } = new();
}

