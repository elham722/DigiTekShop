using DigiTekShop.Application.Common.Events;
using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.Abstractions.Identity.DeviceManagement;
using DigiTekShop.Contracts.Abstractions.Identity.Encryption;
using DigiTekShop.Contracts.Abstractions.Identity.Lockout;
using DigiTekShop.Contracts.Abstractions.Identity.Password;
using DigiTekShop.Contracts.Abstractions.Identity.Permission;
using DigiTekShop.Contracts.Abstractions.Identity.Security;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Identity.Events;
using DigiTekShop.Identity.Interceptors;
using DigiTekShop.Identity.Options.Security;
using DigiTekShop.Identity.Services;
using DigiTekShop.Identity.Services.Register;
using DigiTekShop.Identity.Services.Tokens;
using DigiTekShop.SharedKernel.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DigiTekShop.Identity.DependencyInjection;

public static class IdentityServicesRegistration
{
    public static IServiceCollection ConfigureIdentityCore(this IServiceCollection services, IConfiguration configuration)
    {
        #region DbContext

        services.AddDbContext<DigiTekShopIdentityDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(configuration.GetConnectionString("IdentityDBConnection"));
            var mapper = sp.GetRequiredService<IIntegrationEventMapper>();
            var clock = sp.GetRequiredService<IDateTimeProvider>();
            opt.AddInterceptors(new IdentityOutboxBeforeCommitInterceptor(mapper, clock));
        });

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
            // ⚠️ TODO: Set to true in production after Email service is configured
            options.SignIn.RequireConfirmedEmail = false; // Temporarily disabled for testing
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

        #region Device Limits Settings
        services.Configure<DeviceLimitsSettings>(configuration.GetSection("DeviceLimits"));
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<DeviceLimitsSettings>>().Value);
        #endregion

        #region Security Settings
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<SecuritySettings>>().Value);
        #endregion

        #region Password Policy
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection("PasswordPolicy"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Strong Password Validator
        services.AddTransient<IPasswordValidator<User>, StrongPasswordValidator>();


        #endregion


        #region AddServices


        services.AddScoped<ILoginService, LoginService>();

        services.AddScoped<IRegistrationService, RegistrationService>();

        services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
        services.AddScoped<IPasswordService, PasswordResetService>();    

        services.AddScoped<ITwoFactorService, TwoFactorService>();    
        services.AddScoped<ILockoutService, LockoutService>();

        // Email Confirmation settings + service
        services.Configure<EmailConfirmationSettings>(configuration.GetSection("EmailConfirmation"));
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        // Phone Verification settings + service
        services.Configure<PhoneVerificationSettings>(configuration.GetSection("PhoneVerification"));
        services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();
       

        // Encryption Service for TOTP secrets
        services.AddScoped<IEncryptionService, EncryptionService>();

        // Device Management Service
        services.AddScoped<IDeviceManagementService, DeviceManagementService>();

        // Permission Evaluator Service
        services.AddScoped<IPermissionEvaluatorService, PermissionEvaluatorService>();

        // Login Attempt Service
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();

        // Security Event Service
        services.AddScoped<ISecurityEventService, SecurityEventService>();

        #endregion


        services.AddScoped<IIntegrationEventMapper, IdentityIntegrationEventMapper>();


        return services;
    }

    public static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings section is missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
            throw new InvalidOperationException("JwtSettings:Key is not configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = true;         
                o.SaveToken = true;                     

                o.TokenValidationParameters = new TokenValidationParameters
                {
                    // ✅ Signature Validation
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    RequireSignedTokens = true, // ✅ Only accept signed tokens

                    // ✅ Issuer Validation
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    // ✅ Audience Validation
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    // ✅ Lifetime Validation
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    // ✅ Algorithm Validation (prevent algorithm substitution attacks)
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
                };

                // ✅ Optional: SignalR/WebSocket support (read token from query string)
                // Uncomment if you add SignalR:
                // o.Events = new JwtBearerEvents
                // {
                //     OnMessageReceived = context =>
                //     {
                //         var accessToken = context.Request.Query["access_token"];
                //         var path = context.HttpContext.Request.Path;
                //         if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                //         {
                //             context.Token = accessToken;
                //         }
                //         return Task.CompletedTask;
                //     }
                // };
            });

        return services;
    }



}
