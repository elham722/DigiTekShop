using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender
{
    public interface ITemplateEmailSender : IEmailSender
    {
        Task<Result> SendTemplateEmailAsync(string toEmail, string templateName, object templateData);
    }
}
