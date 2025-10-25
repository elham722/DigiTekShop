using DigiTekShop.Contracts.Abstractions.ExternalServices.EmailSender;
using DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;
using DigiTekShop.ExternalServices.Email.Options;
using DigiTekShop.ExternalServices.Email.Templates;
using DigiTekShop.ExternalServices.Sms;
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


            services.Configure<SmsIrSettings>(configuration.GetSection("SmsIr"));

            services.AddHttpClient(nameof(SmsIrSmsSender), (sp, http) =>
            {
                var opt = sp.GetRequiredService<IOptions<SmsIrSettings>>().Value;

                http.BaseAddress = new Uri(opt.BaseUrl.EndsWith("/") ? opt.BaseUrl : opt.BaseUrl + "/");
                http.Timeout = TimeSpan.FromSeconds(Math.Max(1, opt.TimeoutSeconds));

                if (!string.IsNullOrWhiteSpace(opt.ApiKeyHeaderName) && !string.IsNullOrWhiteSpace(opt.ApiKey))
                {
                    http.DefaultRequestHeaders.Remove(opt.ApiKeyHeaderName);
                    http.DefaultRequestHeaders.Add(opt.ApiKeyHeaderName, opt.ApiKey);
                }

                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.ParseAdd("text/plain");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });

            services.AddScoped<IPhoneSender, SmsIrSmsSender>();




            #endregion

            return services;
        }
    }

}
