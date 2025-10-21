
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DigiTekShop.Contracts.Abstractions.Identity.Device;
using DigiTekShop.Contracts.Abstractions.Identity.Mfa;
using DigiTekShop.Contracts.Options.Auth;
using DigiTekShop.Contracts.Options.Email;
using DigiTekShop.Contracts.Options.Password;
using DigiTekShop.Contracts.Options.Phone;
using DigiTekShop.Contracts.Options.Security;
using DigiTekShop.Contracts.Options.Token;
using DigiTekShop.Identity.Services.Device;
using DigiTekShop.Identity.Services.Logout;
using DigiTekShop.Identity.Services.Mfa;
using DigiTekShop.Identity.Services.Permission;

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
            var corr = sp.GetRequiredService<ICorrelationContext>(); 

            opt.AddInterceptors(new IdentityOutboxBeforeCommitInterceptor(mapper, clock, corr)); 
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

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = true; 
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<DigiTekShopIdentityDbContext>()
        .AddDefaultTokenProviders();

        #endregion

        #region Password Policy
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection("PasswordPolicy"))
            .ValidateDataAnnotations()
            .ValidateOnStart();



        #endregion

        #region AddServices

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<JwtSettings>>().Value);
        services.AddScoped<ITokenService, TokenService>();

        services.Configure<LoginFlowOptions>(configuration.GetSection("Auth:LoginFlow"));
        services.AddScoped<ILoginService, LoginService>();

        services.Configure<PasswordResetOptions>(configuration.GetSection("PasswordReset"));
        services.AddScoped<IPasswordService, PasswordResetService>();

        services.Configure<IdentityLockoutOptions>(configuration.GetSection("Identity:Lockout"));
        services.AddScoped<ILockoutService, LockoutService>();

        services.Configure<EmailConfirmationOptions>(configuration.GetSection("EmailConfirmation"));
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        services.Configure<PhoneVerificationOptions>(configuration.GetSection("PhoneVerification"));
        services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

        services.Configure<LoginAttemptOptions>(configuration.GetSection("Auth:LoginAttempts"));
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();

        services.Configure<DeviceLimitsOptions>(configuration.GetSection("DeviceLimits"));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<DeviceLimitsOptions>>().Value);
        services.AddScoped<IDeviceRegistry, DeviceRegistry>();

        services.Configure<SecurityEventsOptions>(configuration.GetSection("SecurityEvents"));
        services.AddScoped<ISecurityEventService, SecurityEventService>();

        services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

        services.AddScoped<IRegistrationService, RegistrationService>();

        services.AddScoped<IEncryptionService, EncryptionService>();

        services.AddScoped<IPermissionEvaluatorService, PermissionEvaluatorService>();

        services.AddScoped<IIdentityGateway, IdentityGateway>();

        services.AddScoped<IMfaService, MfaService>();

        services.AddScoped<ILogoutService, LogoutService>();

        #endregion

        #region Event & outbox

        services.AddScoped<IdentityIntegrationEventMapper>();

        services.AddScoped<IIntegrationEventHandler<AddCustomerIdIntegrationEvent>, CustomerCreatedHandler>();

        services.AddScoped<IIntegrationEventHandler<UserRegisteredIntegrationEvent>, UserRegisteredNotificationHandler>();

        services.Configure<NotificationFeatureFlags>(configuration.GetSection(NotificationFeatureFlags.SectionName));

        #endregion

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
                o.SaveToken = false;                     

                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    RequireSignedTokens = true, 

                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
                };

                o.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        var blacklist = ctx.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                        var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                        var sub = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                        if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(sub))
                        {
                            ctx.Fail("invalid token payload");
                            return;
                        }

                        if (await blacklist.IsTokenRevokedAsync(jti!, ctx.HttpContext.RequestAborted))
                        {
                            ctx.Fail("access token revoked");
                            return;
                        }

                        var userMgr = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                        var user = await userMgr.FindByIdAsync(sub);
                        if (user is null || user.IsDeleted || !user.EmailConfirmed)
                        {
                            ctx.Fail("user inactive");
                        }
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        ctx.NoResult();
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

}
