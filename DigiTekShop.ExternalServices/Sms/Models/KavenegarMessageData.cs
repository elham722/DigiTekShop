using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.ExternalServices.Sms.Models
{
    public class KavenegarMessageData
    {
        public string MessageId { get; set; } = string.Empty;
        public string Receptor { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
