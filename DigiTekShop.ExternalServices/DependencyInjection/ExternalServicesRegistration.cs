
using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Email;
using DigiTekShop.ExternalServices.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.ExternalServices.Sms.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DigiTekShop.Contracts.DTOs.Auth.EmailSender;


namespace DigiTekShop.ExternalServices.DependencyInjection
{
    public static class ExternalServicesRegistration
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            #region Email&Phone


            // Email (SMTP)
            services.AddTransient<IEmailSender, Email.SmtpEmailSender>();
            services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
           
           


            // SMS (Kavenegar)
            services.Configure<KavenegarSettings>(configuration.GetSection("Kavenegar"));
            services.AddHttpClient<IPhoneSender, Sms.KavenegarSmsSender>(); 

            #endregion
            return services;
        }
    }

}
