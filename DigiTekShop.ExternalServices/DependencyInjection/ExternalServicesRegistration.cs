using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Email.Options;
using DigiTekShop.ExternalServices.Email.Templates;
using DigiTekShop.ExternalServices.Sms.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace DigiTekShop.ExternalServices.DependencyInjection
{
    public static class ExternalServicesRegistration
    {
        public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            #region Email&Phone

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
