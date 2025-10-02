namespace DigiTekShop.ExternalServices.Sms.Models
{
    public class KavenegarSettings
    {
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = "https://api.kavenegar.com/v1";

        public string LineNumber { get; set; } = string.Empty;
    }
}