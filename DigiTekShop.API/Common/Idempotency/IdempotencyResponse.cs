namespace DigiTekShop.API.Common.Idempotency
{
    public class IdempotencyResponse
    {
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty; 
    }
}
