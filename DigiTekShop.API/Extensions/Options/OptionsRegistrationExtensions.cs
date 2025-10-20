using DigiTekShop.Contracts.Options.Api;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Caching;
using DigiTekShop.Contracts.Options.Email;
using DigiTekShop.Contracts.Options.Messaging;
using DigiTekShop.Contracts.Options.Password;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.Contracts.Options.Security;
using DigiTekShop.Contracts.Options.Token;

namespace DigiTekShop.API.Extensions.Options
{
    public static class OptionsRegistrationExtensions
    {
        public static IServiceCollection AddAppOptionsLite(this IServiceCollection services, IConfiguration cfg)
        {
            // Auth
            services.AddOptions<JwtSettings>().Bind(cfg.GetSection("JwtSettings"));
            services.AddOptions<PasswordPolicyOptions>().Bind(cfg.GetSection("PasswordPolicy"));
            services.AddOptions<EmailConfirmationOptions>().Bind(cfg.GetSection("EmailConfirmation"));
            services.AddOptions<PhoneVerificationOptions>().Bind(cfg.GetSection("PhoneVerification"));
            services.AddOptions<PasswordResetOptions>().Bind(cfg.GetSection("PasswordReset"));

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
            services.AddOptions<ApiOptions>().Bind(cfg.GetSection("ApiOptions"));
            services.AddOptions<ReverseProxyOptions>().Bind(cfg.GetSection("ReverseProxy"));

            services.AddOptions<LoginFlowOptions>().Bind(cfg.GetSection("Auth:LoginFlow"));
            services.AddOptions<LoginAttemptOptions>().Bind(cfg.GetSection("Auth:LoginAttempts"));
            services.AddOptions<IdentityLockoutOptions>().Bind(cfg.GetSection("Identity:Lockout"));

            return services;
        }
    }
}
