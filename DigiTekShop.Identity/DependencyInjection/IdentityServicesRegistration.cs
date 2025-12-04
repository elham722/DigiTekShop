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

            options.User.RequireUniqueEmail = false;

            options.SignIn.RequireConfirmedEmail = false; 
            options.SignIn.RequireConfirmedPhoneNumber = true;
        })
        .AddEntityFrameworkStores<DigiTekShopIdentityDbContext>()
        .AddDefaultTokenProviders();

        #endregion

        #region AddServices

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<JwtSettings>>().Value);
        services.AddScoped<ITokenService, TokenService>();

        services.Configure<IdentityLockoutOptions>(configuration.GetSection("Identity:Lockout"));
        services.AddScoped<ILockoutService, LockoutService>();

        services.Configure<EmailConfirmationOptions>(configuration.GetSection("EmailConfirmation"));
        services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

        services.Configure<PhoneVerificationOptions>(configuration.GetSection("PhoneVerification"));
        services.AddScoped<IAuthService, OtpAuthService>();
        services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

        services.Configure<LoginAttemptOptions>(configuration.GetSection("Auth:LoginAttempts"));
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();

        services.Configure<DeviceLimitsOptions>(configuration.GetSection("DeviceLimits"));
        services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<DeviceLimitsOptions>>().Value);
        services.AddScoped<IDeviceRegistry, DeviceRegistry>();

        services.Configure<SecurityEventsOptions>(configuration.GetSection("SecurityEvents"));
        services.AddScoped<ISecurityEventService, SecurityEventService>();



        services.AddScoped<IEncryptionService, EncryptionService>();

        services.AddScoped<IPermissionEvaluatorService, PermissionEvaluatorService>();

        services.AddScoped<IIdentityGateway, IdentityGateway>();

        services.AddScoped<ILogoutService, LogoutService>();

        services.AddScoped<IMeService, MeService>();
        services.AddScoped<IAdminUserReadService, AdminUserReadService>();

        services.AddScoped<IUserProfileService, UserProfileService>();

        #endregion

        #region Event & outbox

        services.AddScoped<IdentityIntegrationEventMapper>();

        services.AddScoped<IIntegrationEventHandler<AddCustomerIdIntegrationEvent>, CustomerCreatedHandler>();

        services.AddScoped<IIntegrationEventHandler<PhoneVerificationIssuedIntegrationEvent>, PhoneVerificationIssuedHandler>();

        services.Configure<NotificationFeatureFlags>(configuration.GetSection(NotificationFeatureFlags.SectionName));

        #endregion

        return services;
    }

    public static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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
                    OnMessageReceived = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuthentication");

                        var auth = ctx.Request.Headers.Authorization.ToString();
                        var hasAuth = !string.IsNullOrWhiteSpace(auth);

                        if (ctx.Request.Path.StartsWithSegments("/api/v1/Auth"))
                        {
                            logger.LogInformation("[JWT] Auth request. Path={Path}, HasAuthHeader={HasAuth}, HeaderPreview={Preview}",
                                ctx.Request.Path,
                                hasAuth,
                                hasAuth ? auth[..Math.Min(30, auth.Length)] + "..." : "(none)");
                        }

                        if (hasAuth)
                        {
                            const string prefix = "Bearer ";
                            if (auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                var raw = auth[prefix.Length..].Trim();

                                if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                    raw = raw[prefix.Length..].Trim();

                                if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
                                    raw = raw[1..^1];

                                ctx.Token = raw;
                            }
                        }

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = async ctx =>
                    {
                        var sp = ctx.HttpContext.RequestServices;
                        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuthentication");
                        var blacklist = sp.GetRequiredService<ITokenBlacklistService>();

                        var p = ctx.Principal!;

                        // JTI با fallback
                        var jti = p.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                                  ?? p.FindFirst("jti")?.Value;

                        // SUB با fallbackهای متعدد
                        var sub = p.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                  ?? p.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? p.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                                  ?? p.FindFirst("sub")?.Value;

                        logger.LogDebug("[JWT] Token validated. JTI={Jti}, SUB={Sub}", jti, sub);

                        if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(sub))
                        {
                            logger.LogWarning("[JWT] Invalid token payload - missing JTI or SUB");
                            ctx.Fail("invalid token payload");
                            return;
                        }

                        // JTI blacklist
                        if (await blacklist.IsTokenRevokedAsync(jti, ctx.HttpContext.RequestAborted))
                        {
                            logger.LogWarning("[JWT] Access token revoked. JTI={Jti}", jti);
                            ctx.Fail("access token revoked");
                            return;
                        }

                        // (اختیاری) چک logout-all با iat
                        var iatStr = p.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                        if (Guid.TryParse(sub, out var userId) && long.TryParse(iatStr, out var iatUnix))
                        {
                            var iatUtc = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
                            if (await blacklist.IsUserTokensRevokedAsync(userId, iatUtc, ctx.HttpContext.RequestAborted))
                            {
                                logger.LogWarning("[JWT] User tokens revoked after iat. user={UserId}", userId);
                                ctx.Fail("user tokens revoked");
                                return;
                            }
                        }

                        // Active user check
                        var userMgr = sp.GetRequiredService<UserManager<User>>();
                        var user = await userMgr.FindByIdAsync(sub);
                        if (user is null || user.IsDeleted || !(user.EmailConfirmed || user.PhoneNumberConfirmed))
                        {
                            ctx.Fail("user inactive");
                            return;
                        }

                    },

                    OnAuthenticationFailed = ctx =>
                    {
                        var loggerFactory = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("JwtAuthentication");
                        logger.LogWarning("[JWT] Authentication failed. Exception={Exception}, Failure={Failure}", 
                            ctx.Exception?.Message, ctx.Result?.Failure?.ToString());
                        ctx.NoResult();
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

}
