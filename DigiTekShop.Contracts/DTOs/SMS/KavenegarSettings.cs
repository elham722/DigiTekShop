// DigiTekShop.ExternalServices.Sms.Models.KavenegarSettings
namespace DigiTekShop.Contracts.DTOs.SMS
{
    public class KavenegarSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.kavenegar.com/v1";
        public string DefaultSender { get; set; } = string.Empty;   // ← به جای LineNumber
        public string OtpTemplate { get; set; } = "login-otp";      // optional: اگر از lookup استفاده کنی
        public int TimeoutSeconds { get; set; } = 10;
    }
}