using Asp.Versioning;
using DigiTekShop.API.Errors;
using DigiTekShop.API.Extensions;
using DigiTekShop.API.Middleware;
using DigiTekShop.Application.DependencyInjection;
using DigiTekShop.ExternalServices.DependencyInjection;
using DigiTekShop.Identity.DependencyInjection;
using DigiTekShop.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================
// Constants / Shared strings
// ============================
const string CorrelationHeader = "X-Request-ID"; // ← هدر واحد برای Correlation

#region Logging Configuration

// ✅ Serilog Configuration
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DigiTekShop.API")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

#endregion

// ✅ Kestrel soft limits (DoS نرم)
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// ✅ ForwardedHeaders (قبل از Build و مطابق appsettings.Production)
builder.Services.AddForwardedHeadersSupport(builder.Configuration);

#region CORS Configuration

// ✅ CORS Policy
builder.Services.AddCors(options =>
{
    // Development: Allow all
    options.AddPolicy("Development", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());

    // Production: Restrict to specific origins
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

builder.Services.AddControllers()
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
    // Global rate limiter (per user or IP)
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

    // Auth endpoints: strict
    options.AddFixedWindowLimiter("AuthPolicy", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    // API endpoints: moderate
    options.AddFixedWindowLimiter("ApiPolicy", options =>
    {
        options.PermitLimit = 50;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    // ✅ On rejection → 429 + Retry-After (اختیاری: تمیزتر با null)
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

// ✅ Infrastructure services (Redis, Caching, DataProtection)
builder.Services.AddInfrastructure(builder.Configuration);

// ✅ Identity and Authentication
builder.Services
    .ConfigureIdentityCore(builder.Configuration)
    .ConfigureJwtAuthentication(builder.Configuration);

// ✅ External services (Email, SMS)
builder.Services.AddExternalServices(builder.Configuration);

// ✅ Application layer (MediatR, FluentValidation)
builder.Services.ConfigureApplicationCore();

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

#region Health Checks

builder.Services.AddComprehensiveHealthChecks();

#endregion

#region Error Handling

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

#endregion

#region Performance Optimizations

// ✅ Response Compression (Gzip, Brotli)
builder.Services.AddResponseCompressionOptimized();

// ✅ Output Caching
builder.Services.AddOutputCachingOptimized();

// ✅ Memory Cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.CompactionPercentage = 0.2;
});

// ✅ HTTP Client optimizations
builder.Services.AddHttpClientOptimized();

#endregion

// ============================================================
// Build Application
// ============================================================

var app = builder.Build();

// ============================================================
// Middleware Pipeline (ORDER MATTERS!)
// ============================================================

#region Exception Handling

// ✅ Exception handler (must be first)
app.UseExceptionHandler();

#endregion

// ✅ Forwarded headers (قبل از هر چیزی که به IP/Schema نیاز دارد)
app.UseForwardedHeadersSupport(builder.Configuration);

// ✅ Correlation ID (هدر واحد)
app.UseCorrelationId(headerName: CorrelationHeader);

#region Logging

// ✅ Serilog request logging
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

// ✅ Request logging middleware (development only)
if (app.Environment.IsDevelopment())
{
    app.UseRequestLogging();
}

#endregion

#region Security

// ✅ HSTS (production only)
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

// ✅ HTTPS Redirection
app.UseHttpsRedirection();

// ✅ Security Headers
app.UseSecurityHeaders();

// ✅ CORS
app.UseCors(app.Environment.IsProduction() ? "Production" : "Development");

#endregion

#region Performance

// ✅ Response Compression
app.UseResponseCompression();

// ✅ Output Caching
app.UseOutputCache();

#endregion

#region Routing & Rate Limiting

app.UseRouting();

// ✅ Rate Limiting (before authentication)
app.UseRateLimiter();

#endregion

#region Authentication & Authorization

// ✅ Authentication (JWT)
app.UseAuthentication();

// ✅ Authorization
app.UseAuthorization();

#endregion

#region Swagger (Development Only)

app.UseModernSwagger(app.Environment);

#endregion

#region Endpoints

// ✅ Map controllers
app.MapControllers();

// ✅ Health check endpoints
app.MapHealthCheckEndpoints();

#endregion

// ============================================================
// Run Application
// ============================================================

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
