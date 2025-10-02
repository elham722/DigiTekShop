using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.DTOs.EmailConfirmation
{
    public sealed class EmailTemplateSettings
    {
        public string CompanyName { get; set; } = "DigiTekShop";

        public string SupportEmail { get; set; } = "support@digitekshop.com";

        public string LogoUrl { get; set; } = string.Empty;

        public string PrimaryColor { get; set; } = "#007bff";

        public string ContactUrl { get; set; } = string.Empty;
    }
}
