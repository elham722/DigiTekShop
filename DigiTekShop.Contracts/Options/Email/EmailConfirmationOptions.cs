namespace DigiTekShop.Contracts.Options.Email
{
    public sealed class EmailConfirmationOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ConfirmEmailPath { get; set; } = "api/v1/account/confirm-email";
        public TimeSpan TokenValidity { get; set; } = TimeSpan.FromHours(24);
        public bool RequireEmailConfirmation { get; set; } = true;
        public bool AllowResendConfirmation { get; set; } = true;
        public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromMinutes(5);
        public EmailTemplateOptions Template { get; init; } = new();
    }
}
