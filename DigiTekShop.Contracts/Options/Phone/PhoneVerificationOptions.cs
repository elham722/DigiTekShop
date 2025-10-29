namespace DigiTekShop.Contracts.Options.Phone;

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

    public string CodeHashSecret { get; set; } = "CHANGE_ME_SUPER_SECRET";
    public int TrustDeviceDays { get; set; } = 30;

    public int MaxSendPerWindow { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
    public int MaxVerifyPerWindow { get; set; } = 6;         
    public int VerifyWindowSeconds { get; set; } = 900;
}