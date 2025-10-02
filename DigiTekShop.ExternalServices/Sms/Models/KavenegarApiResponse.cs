using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.ExternalServices.Sms.Models
{
    public class KavenegarApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public KavenegarMessageData[]? Messages { get; set; }
    }
}
