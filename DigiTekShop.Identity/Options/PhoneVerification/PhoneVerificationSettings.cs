namespace DigiTekShop.Identity.Options.PhoneVerification;

public sealed class PhoneVerificationSettings
{
    public int CodeLength { get; set; } = 6;
    public TimeSpan CodeValidity { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; set; } = 5;
    public bool RequirePhoneConfirmation { get; set; } = true;
    public bool AllowResendCode { get; set; } = true;
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromMinutes(2);
    public PhoneSecuritySettings Security { get; set; } = new();
    public PhoneTemplateSettings Template { get; set; } = new();
}