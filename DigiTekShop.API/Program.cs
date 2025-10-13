var builder = WebApplication.CreateBuilder(args);

#region Logging & Options

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DigiTekShop.API")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "false");

#endregion

#region Kestrel Limits

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

#endregion


#region Options & Infrastructure

const string CorrelationHeader = "X-Request-ID";
builder.Services.AddAppOptionsLite(builder.Configuration);

builder.Services.AddForwardedHeadersSupport(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());

    options.AddPolicy("Production", p => p
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .WithHeaders("Authorization", "Content-Type", "X-Device-Id", CorrelationHeader)
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
        .WithExposedHeaders("X-Pagination", CorrelationHeader)
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

builder.Services.AddControllers(o =>
    {
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        o.ModelBinderProviders.Insert(0, new TrimmingModelBinderProvider(convertEmptyToNull: true));
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

#endregion

#region Security: API Key, Rate Limiting 

builder.Services.AddApiKeyAuth(builder.Configuration);
builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection("Idempotency"));

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.Identity?.Name;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(userId ?? ip,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.AddFixedWindowLimiter("AuthPolicy", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("ApiPolicy", o =>
    {
        o.PermitLimit = 50;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 5;
    });

    options.AddFixedWindowLimiter("WebhooksPolicy", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });

    static ValueTask OnRateLimitRejected(OnRejectedContext context, CancellationToken token)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        double? retryAfterSec = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
            retryAfterSec = retryAfter.TotalSeconds;
        }

        var payload = new
        {
            error = "Too many requests. Please try again later.",
            code = "RATE_LIMIT_EXCEEDED",
            retryAfter = retryAfterSec
        };

        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(payload, token));
    }

    options.OnRejected = OnRateLimitRejected;

});

#endregion

#region Infra & App Layers

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services
    .ConfigureIdentityCore(builder.Configuration)
    .ConfigureJwtAuthentication(builder.Configuration);

builder.Services.AddExternalServices(builder.Configuration);
builder.Services.ConfigureApplicationCore();

#endregion


#region Swagger

builder.Services.AddSwaggerMinimal(includeXmlComments: true);
builder.Services.AddEndpointsApiExplorer();

#endregion


#region Https / HSTS

builder.Services.AddHttpsRedirection(o =>
{
    o.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    o.HttpsPort = 443;
});
builder.Services.AddHsts(o =>
{
    o.Preload = true;
    o.IncludeSubDomains = true;
    o.MaxAge = TimeSpan.FromDays(365);
});

#endregion

#region API Versioning

builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-API-Version"),
            new QueryStringApiVersionReader("api-version"));
    })
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";
        o.SubstituteApiVersionInUrl = true;
    });

#endregion

#region Authorization Policies 

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(o =>
{
    o.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

#endregion


#region Health Checks

builder.Services.AddComprehensiveHealthChecks();
builder.Services.AddHealthChecks().AddCheck<SmtpHealthCheck>("smtp_config");

#endregion

#region Error Handling

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

#endregion


#region Performance

builder.Services.AddResponseCompressionOptimized();
builder.Services.AddOutputCachingOptimized();
builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = 1024;
    o.CompactionPercentage = 0.2;
});
builder.Services.AddHttpClientOptimized(builder.Configuration);


#endregion


#region Client Context

builder.Services.AddClientContext();

var app = builder.Build();

#endregion

#region Seed (Dev only)

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await PermissionSeeder.SeedAllAsync(db, logger);
        logger.LogInformation("✅ Permissions and Roles seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding permissions");
    }
}

#endregion


#region Pipeline 


app.UseForwardedHeadersSupport(builder.Configuration);


app.UseExceptionHandler();


app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms | TraceId: {TraceId}";
    opts.EnrichDiagnosticContext = (dc, http) =>
    {
        dc.Set("TraceId", http.TraceIdentifier);
        dc.Set("RequestHost", http.Request.Host.Value);
        dc.Set("RemoteIP", http.Connection.RemoteIpAddress?.ToString());
        if (http.User?.Identity?.IsAuthenticated == true)
            dc.Set("UserName", http.User.Identity!.Name!);
    };
});


app.UseCorrelationId(headerName: CorrelationHeader);
app.UseClientContext();


if (app.Environment.IsProduction())
    app.UseHsts();
app.UseHttpsRedirection();


app.UseSecurityHeaders();


app.UseRouting();


app.UseCors(app.Environment.IsProduction() ? "Production" : "Development");


app.UseRateLimiter();


if (builder.Configuration.GetValue<bool>("ApiKey:Enabled"))
    app.UseApiKeyAuth();

app.UseAuthentication();
app.UseAuthorization();


app.UseIdempotency();


app.UseResponseCompression();
app.UseOutputCache();


app.UseNoStoreForAuth();


app.UseSwaggerMinimal(app.Environment);


app.MapHealthCheckEndpoints();
app.MapControllers();

#endregion

try
{
    Log.Information("Starting DigiTekShop API...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
