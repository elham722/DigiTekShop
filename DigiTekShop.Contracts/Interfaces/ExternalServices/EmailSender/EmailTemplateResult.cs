using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender
{
    public record EmailTemplateResult(string Subject, string HtmlContent, string PlainTextContent);
}
