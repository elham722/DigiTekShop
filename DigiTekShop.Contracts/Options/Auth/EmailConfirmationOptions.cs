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
        public string BaseUrl { get; init; } = "";
        public string ConfirmEmailPath { get; init; } = "account/confirm-email";
        public int TokenValidityMinutes { get; init; } = 1440;
        public bool RequireEmailConfirmation { get; init; } = true;
        public bool AllowResendConfirmation { get; init; } = true;
        public int ResendCooldownMinutes { get; init; } = 5;
        public EmailTemplateOptions Template { get; init; } = new();
    }

    public sealed class EmailTemplateOptions
    {
        public string CompanyName { get; init; } = "DigiTekShop";
        public string SupportEmail { get; init; } = "";
        public string LogoUrl { get; init; } = "";
        public string PrimaryColor { get; init; } = "#007bff";
        public string ContactUrl { get; init; } = "";
    }
}
