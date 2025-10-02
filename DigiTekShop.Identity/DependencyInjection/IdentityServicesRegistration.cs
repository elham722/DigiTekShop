using DigiTekShop.Contracts.DTOs.EmailConfirmation;
using DigiTekShop.Contracts.DTOs.EmailSender;
using DigiTekShop.Contracts.DTOs.JwtSettings;
using DigiTekShop.Contracts.DTOs.PhoneVerification;
using DigiTekShop.Contracts.DTOs.Auth;
using DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;
using DigiTekShop.Contracts.Interfaces.Identity;
using DigiTekShop.Identity.Options;
using DigiTekShop.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DigiTekShop.Contracts.Interfaces.ExternalServices.EmailSender;

namespace DigiTekShop.Identity.DependencyInjection;

public static class IdentityServicesRegistration
{
    public static IServiceCollection ConfigureIdentityCore(this IServiceCollection services, IConfiguration configuration)
    {
        #region DbContext

        services.AddDbContext<DigiTekShopIdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IdentityDBConnection")));

        #endregion

        #region Identity

        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            // SignIn settings
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<DigiTekShopIdentityDbContext>()
        .AddDefaultTokenProviders();

        #endregion

        #region JwtSettings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<JwtSettings>>().Value);

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        #endregion

        #region Password Policy
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection("PasswordPolicy"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Strong Password Validator
        services.AddTransient<IPasswordValidator<User>, StrongPasswordValidator>();

        // Password History Service
        services.AddScoped<PasswordHistoryService>();

        #endregion

        // Email Confirmation settings + service
        services.Configure<EmailConfirmationSettings>(configuration.GetSection("EmailConfirmation"));
        services.AddScoped<EmailConfirmationService>();

        // Phone Verification settings + service
        services.Configure<PhoneVerificationSettings>(configuration.GetSection("PhoneVerification"));
        services.AddScoped<PhoneVerificationService>();

        // Password Reset settings + service
        services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
        services.AddScoped<PasswordResetService>();

        return services;
    }

    public static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidateActor = false
                };
            });

        return services;
    }


}
