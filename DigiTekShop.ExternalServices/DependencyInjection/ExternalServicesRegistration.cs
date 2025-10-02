using DigiTekShop.Contracts.DTOs.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.EmailSender;
using DigiTekShop.Contracts.DTOs.PhoneVerification;
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


namespace DigiTekShop.ExternalServices.DependencyInjection
{
    public static class ExternalServicesRegistration
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            #region Email&Phone

            // Email (SMTP)
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.AddTransient<IEmailSender, Email.SmtpEmailSender>();

            // SMS (Kavenegar)
            services.Configure<KavenegarSettings>(configuration.GetSection("KavenegarSettings"));
            services.AddHttpClient<IPhoneSender, Sms.KavenegarSmsSender>(); // ✅ بدون IConfigurationSection

            #endregion
            return services;
        }
    }

}
