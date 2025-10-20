namespace DigiTekShop.Contracts.Options.Email;
    public sealed class EmailTemplateOptions
    {
        public string CompanyName { get; set; } = "DigiTekShop";

        public string SupportEmail { get; set; } = "support@digitekshop.com";

        public string LogoUrl { get; set; } = string.Empty;

        public string PrimaryColor { get; set; } = "#007bff";

        public string ContactUrl { get; set; } = string.Empty;
    }
