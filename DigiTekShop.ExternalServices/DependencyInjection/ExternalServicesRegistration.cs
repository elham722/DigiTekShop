
using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DigiTekShop.Contracts.DTOs.SMS;
using DigiTekShop.ExternalServices.Email.Options;
using DigiTekShop.ExternalServices.Email.Templates;


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




            services.Configure<KavenegarSettings>(configuration.GetSection("Kavenegar"));
            services.AddHttpClient<IPhoneSender, Sms.KavenegarSmsSender>((sp, client) =>
            {
                var cfg = sp.GetRequiredService<IOptions<KavenegarSettings>>().Value;
                
                client.BaseAddress = new Uri($"{cfg.BaseUrl.TrimEnd('/')}/{cfg.ApiKey}/");
                client.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds > 0 ? cfg.TimeoutSeconds : 10);
            });



            #endregion
            return services;
        }
    }

}
