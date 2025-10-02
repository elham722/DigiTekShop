using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender
{
    public record PhoneCodeRequest(string PhoneNumber, string Code, string? TemplateName = null);
}
