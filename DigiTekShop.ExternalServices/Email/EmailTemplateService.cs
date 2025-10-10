using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.ExternalServices.Email
{
    public sealed class EmailTemplateService : IEmailTemplateService
    {
        public EmailConfirmationContent BuildEmailConfirmation(string confirmUrl, string companyName = "DigiTekShop")
        {
            var subject = $"تأیید ایمیل - {companyName}";
            var plain = $"برای تأیید ایمیل روی لینک زیر بزن:\n{confirmUrl}";
            var html = $@"<!doctype html><html lang=""fa""><body style=""font-family:tahoma,arial"">
            <h3>تأیید ایمیل</h3>
            <p>برای تکمیل ثبت‌نام، ایمیل خود را تأیید کن.</p>
            <p><a href=""{System.Net.WebUtility.HtmlEncode(confirmUrl)}""
                  style=""background:#007bff;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none"">تأیید ایمیل</a></p>
            <p>اگر دکمه کار نکرد، لینک: {System.Net.WebUtility.HtmlEncode(confirmUrl)}</p>
        </body></html>";
            return new EmailConfirmationContent(subject, html, plain);
        }
    }

}
