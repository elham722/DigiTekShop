using DigiTekShop.Contracts.Options.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Options.Auth
{
    public sealed class EmailConfirmationOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ConfirmEmailPath { get; set; } = "account/confirm-email";
        public TimeSpan TokenValidity { get; set; } = TimeSpan.FromHours(24);
        public bool RequireEmailConfirmation { get; set; } = true;
        public bool AllowResendConfirmation { get; set; } = true;
        public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromMinutes(5);
        public EmailTemplateOptions Template { get; init; } = new();
    }

    public sealed class EmailTemplateOptions
    {
        public string CompanyName { get; set; } = "DigiTekShop";

        public string SupportEmail { get; set; } = "support@digitekshop.com";

        public string LogoUrl { get; set; } = string.Empty;

        public string PrimaryColor { get; set; } = "#007bff";

        public string ContactUrl { get; set; } = string.Empty;
    }
}
