using DigiTekShop.Contracts.Options.Api;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Caching;
using DigiTekShop.Contracts.Options.Messaging;
using DigiTekShop.Contracts.Options.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.API.Extensions.Options;


public static class OptionsValidationExtensions
{
    public static IServiceCollection AddOptionsValidation(this IServiceCollection services, IConfiguration configuration)
    {
        #region Auth

        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("JwtSettings"))
            .ValidateDataAnnotations().Validate(s => s.AccessTokenExpirationMinutes < (s.RefreshTokenExpirationDays * 24 * 60),
                "AccessToken must be shorter than RefreshToken").ValidateOnStart();

        services.AddOptions<PasswordPolicyOptions>().Bind(configuration.GetSection("PasswordPolicy")).ValidateOnStart();
        services.AddOptions<EmailConfirmationOptions>().Bind(configuration.GetSection("EmailConfirmation")).ValidateOnStart();
        services.AddOptions<PhoneVerificationOptions>().Bind(configuration.GetSection("PhoneVerification")).ValidateOnStart();
        services.AddOptions<PasswordResetOptions>().Bind(configuration.GetSection("PasswordReset")).ValidateOnStart();

        #endregion

        #region Caching / Redis

        services.AddOptions<RedisCacheOptions>().Bind(configuration.GetSection("RedisCache")).ValidateOnStart();
        services.AddOptions<RedisConnectionOptions>().Configure(o =>
            {
                o.ConnectionString = configuration.GetConnectionString("Redis") ?? string.Empty;
            }).Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis ConnectionString is required")
            .ValidateOnStart();

        #endregion

        #region Messaging

        services.AddOptions<EmailOptions>().Bind(configuration.GetSection("Email")).ValidateOnStart();
        services.AddOptions<SmtpSettings>().Bind(configuration.GetSection("SmtpSettings")).ValidateOnStart();
        services.AddOptions<SmsOptions>().Bind(configuration.GetSection("Sms")).ValidateOnStart();
        services.AddOptions<KavenegarOptions>().Bind(configuration.GetSection("Kavenegar")).ValidateOnStart();

        #endregion

        #region Security

        services.AddOptions<DeviceLimitsOptions>().Bind(configuration.GetSection("DeviceLimits")).ValidateOnStart();
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection("Security")).ValidateOnStart();

        #endregion

        #region API

        services.AddOptions<ApiOptions>().Bind(configuration.GetSection("ApiOptions")).ValidateOnStart();
        services.AddOptions<ReverseProxyOptions>().Bind(configuration.GetSection("ReverseProxy")).ValidateOnStart();

        #endregion


        return services;
    }
}


