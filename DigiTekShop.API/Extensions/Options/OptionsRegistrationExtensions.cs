using DigiTekShop.Contracts.Options.Api;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Caching;
using DigiTekShop.Contracts.Options.Email;
using DigiTekShop.Contracts.Options.Messaging;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.Contracts.Options.Security;
using DigiTekShop.Contracts.Options.Token;
using ApiBehaviorOptions = Microsoft.AspNetCore.Mvc.ApiBehaviorOptions;

namespace DigiTekShop.API.Extensions.Options
{
    public static class OptionsRegistrationExtensions
    {
        public static IServiceCollection AddAppOptionsLite(this IServiceCollection services, IConfiguration cfg)
        {
            // Auth
            services.AddOptions<JwtSettings>().Bind(cfg.GetSection("JwtSettings"));
            services.AddOptions<EmailConfirmationOptions>().Bind(cfg.GetSection("EmailConfirmation"));
            services.AddOptions<PhoneVerificationOptions>().Bind(cfg.GetSection("PhoneVerification"));

            // Caching / Redis
            services.AddOptions<RedisCacheOptions>().Bind(cfg.GetSection("RedisCache"));
            services.Configure<RedisConnectionOptions>(o =>
                o.ConnectionString = cfg.GetConnectionString("Redis") ?? string.Empty);

            // Messaging
            services.AddOptions<EmailOptions>().Bind(cfg.GetSection("Email"));
            services.AddOptions<SmtpSettings>().Bind(cfg.GetSection("SmtpSettings"));
            services.AddOptions<SmsOptions>().Bind(cfg.GetSection("Sms"));
            services.AddOptions<KavenegarOptions>().Bind(cfg.GetSection("Kavenegar"));

            // Security
            services.AddOptions<DeviceLimitsOptions>().Bind(cfg.GetSection("DeviceLimits"));

            // API
            services.AddOptions<ApiMetadataOptions>().Bind(cfg.GetSection("Api:Metadata"));
            services.AddOptions<ApiBehaviorOptions>().Bind(cfg.GetSection("Api:Behavior"));

            services.AddOptions<ReverseProxyOptions>().Bind(cfg.GetSection("ReverseProxy"));

            services.AddOptions<LoginFlowOptions>().Bind(cfg.GetSection("Auth:LoginFlow"));
            services.AddOptions<LoginAttemptOptions>().Bind(cfg.GetSection("Auth:LoginAttempts"));
            services.AddOptions<IdentityLockoutOptions>().Bind(cfg.GetSection("Identity:Lockout"));
            services.AddOptions<SecurityEventsOptions>().Bind(cfg.GetSection("SecurityEvents"));

            return services;
        }
    }
}
