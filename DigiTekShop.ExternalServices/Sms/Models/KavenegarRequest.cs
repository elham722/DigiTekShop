namespace DigiTekShop.ExternalServices.Sms.Models
{
    public class KavenegarRequest
    {
        public string Receptor { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }
}