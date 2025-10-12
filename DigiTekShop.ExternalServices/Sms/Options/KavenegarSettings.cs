namespace DigiTekShop.ExternalServices.Sms.Options;

public class KavenegarSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.kavenegar.com/v1";
    public string DefaultSender { get; set; } = string.Empty;
    public string OtpTemplate { get; set; } = "login-otp";
    public int TimeoutSeconds { get; set; } = 10;
}


