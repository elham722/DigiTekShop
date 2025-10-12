using Asp.Versioning;
using DigiTekShop.API.Errors;
using DigiTekShop.API.Extensions;
using DigiTekShop.API.Middleware;
using DigiTekShop.Application.DependencyInjection;
using DigiTekShop.ExternalServices.DependencyInjection;
using DigiTekShop.ExternalServices.Email;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Data;
using DigiTekShop.Identity.DependencyInjection;
using DigiTekShop.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DigiTekShop.Persistence.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "false");

const string CorrelationHeader = "X-Request-ID";

#region Logging Configuration

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DigiTekShop.API")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

#endregion


builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; 
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});


builder.Services.AddForwardedHeadersSupport(builder.Configuration);

#region CORS Configuration


builder.Services.AddCors(options =>
{
   
    options.AddPolicy("Development", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());

    
    options.AddPolicy("Production", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .WithHeaders("Authorization", "Content-Type", "X-Device-Id", CorrelationHeader)
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
        .WithExposedHeaders("X-Pagination", CorrelationHeader)
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});

#endregion

#region Controllers & JSON Configuration

builder.Services.AddControllers(options =>
    {
        options.ModelBinderProviders.Insert(0, new TrimmingModelBinderProvider(convertEmptyToNull: true));
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

#endregion

#region Rate Limiting

builder.Services.AddRateLimiter(options =>
{
    
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.Identity?.Name;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = userId ?? ipAddress;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    
    options.AddFixedWindowLimiter("AuthPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("ApiPolicy", options =>
    {
        options.PermitLimit = 50;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        double? retryAfterSec = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
            retryAfterSec = retryAfter.TotalSeconds;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            code = "RATE_LIMIT_EXCEEDED",
            retryAfter = retryAfterSec
        }, cancellationToken);
    };
});

#endregion


#region Infrastructure & Application Services

// Add options validation
builder.Services.AddOptionsValidation(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration,builder.Environment);
builder.Services.AddPersistenceServices(builder.Configuration);

builder.Services
    .ConfigureIdentityCore(builder.Configuration)
    .ConfigureJwtAuthentication(builder.Configuration);

builder.Services.AddExternalServices(builder.Configuration);

builder.Services.ConfigureApplicationCore();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies();
});

#endregion

#region API Documentation (Swagger/OpenAPI)

builder.Services.AddModernSwagger(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

#endregion

#region HTTPS & HSTS

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
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
        new QueryStringApiVersionReader("api-version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

#endregion

#region Authorization Policies

builder.Services.AddAuthorization(options =>
{
    
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
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

#region Performance Optimizations


builder.Services.AddResponseCompressionOptimized();


builder.Services.AddOutputCachingOptimized();


builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.CompactionPercentage = 0.2;
});


builder.Services.AddHttpClientOptimized();

#endregion



var app = builder.Build();

// Seed permissions and roles on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DigiTekShopIdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await PermissionSeeder.SeedAllAsync(context, logger);
        logger.LogInformation("✅ Permissions and Roles seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error seeding permissions");
    }
}

app.UseForwardedHeadersSupport(builder.Configuration);


#region Exception Handling

app.UseExceptionHandler();

#endregion


app.UseCorrelationId(headerName: CorrelationHeader);

app.UseApiKey();

#region Logging


app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms | TraceId: {TraceId}";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());

        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
        }
    };
});


if (app.Environment.IsDevelopment())
{
    app.UseRequestLogging();
}

#endregion

#region Security


if (app.Environment.IsProduction())
{
    app.UseHsts();
}


app.UseHttpsRedirection();


app.UseSecurityHeaders();


app.UseCors(app.Environment.IsProduction() ? "Production" : "Development");

#endregion

#region Performance


app.UseResponseCompression();


app.UseOutputCache();

#endregion

#region Routing & Rate Limiting

app.UseRouting();


app.UseRateLimiter();

#endregion

#region Authentication & Authorization


app.UseAuthentication();


app.UseAuthorization();

#endregion

#region Swagger (Development Only)

app.UseModernSwagger(app.Environment);

#endregion

#region Endpoints

app.UseMiddleware<NoStoreAuthMiddleware>();
app.UseIdempotency(); // Add idempotency middleware
app.MapControllers();

// ✅ Health check endpoints
app.MapHealthCheckEndpoints();

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
