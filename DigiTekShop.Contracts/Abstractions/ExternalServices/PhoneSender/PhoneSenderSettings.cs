namespace DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender
{
    public sealed class PhoneSenderSettings
    {
        public string ProviderName { get; set; } = string.Empty;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public int TimeoutMs { get; set; } = 30000;
        public bool LogMessageContent { get; set; } = false;
        public Dictionary<string, string> ProviderSettings { get; set; } = new();
    }
}
