using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Kestrel Limits (File Upload, etc.)
// ========================================
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// ========================================
// Services
// ========================================

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDataProtection()
    .SetApplicationName("DigiTekShop.MVC")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Cookie Authentication برای UI (بدون TokenStore/Refresh)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        
        // Cookie Security
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.Path = "/";
        options.Cookie.Name = ".DigiTekShop.Auth";
    });

builder.Services.AddAuthorization();

// Antiforgery (فقط برای MVC Forms، نه API calls)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.FormFieldName = "__RequestVerificationToken";
    options.Cookie.Name = "__Host-DTS.AntiXsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Response Compression (برای JSON/HTML)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/javascript",
        "text/css",
        "text/html"
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// ========================================
// YARP Reverse Proxy: همه /api/* به Backend API
// ========================================
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] 
    ?? throw new InvalidOperationException("ApiBaseUrl is required in appsettings.json");

var routes = new[]
{
    new RouteConfig
    {
        RouteId = "api-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/api/{**catch-all}"
        }
    }
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "api-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend"] = new DestinationConfig
            {
                Address = apiBaseUrl.TrimEnd('/')
            }
        },
        // Health Check (Timeout در Kestrel تنظیم شده)
        HealthCheck = new HealthCheckConfig
        {
            Passive = new PassiveHealthCheckConfig
            {
                Enabled = true, // اگر 503 بیاد، destination را موقتاً غیرفعال کن
                Policy = "TransientFailurePolicy"
            }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms(builderContext =>
    {
        // Forward کردن TraceIdentifier به‌عنوان Correlation ID
        builderContext.AddRequestHeader("X-Request-ID", "{TraceIdentifier}", append: false);
        
        // Copy کردن همه headers (Cookie, Device-Id, etc.)
        // به‌صورت پیش‌فرض YARP این کار را می‌کند، اما اینجا صریح می‌نویسیم
        // اگر نیاز به modify کردن header خاصی داری، اینجا اضافه کن
        
        // مثال: اگر زبان را از Cookie می‌خوانی و می‌خواهی به API بفرستی:
        // builderContext.AddRequestTransform(async context =>
        // {
        //     if (context.HttpContext.Request.Cookies.TryGetValue("lang", out var lang))
        //     {
        //         context.ProxyRequest.Headers.TryAddWithoutValidation("Accept-Language", lang);
        //     }
        // });
    });

// ========================================
// App Pipeline
// ========================================

var app = builder.Build();

// ========================================
// Forwarded Headers (برای IP واقعی پشت Proxy/LoadBalancer)
// ========================================
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // در production اگر پشت nginx/cloudflare هستی، KnownProxies/KnownNetworks تنظیم کن
    ForwardLimit = 1 // تعداد proxy های قابل اعتماد
});

// Exception Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Response Compression
app.UseResponseCompression();

// Security Headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // CSP (Content Security Policy) - سفارشی‌سازی بر اساس نیاز
    if (!app.Environment.IsDevelopment())
    {
        headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // اگر inline scripts داری
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'";
    }
    
    await next();
});

// Cache Headers برای Auth/Account (no-store)
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase) ||
        ctx.Request.Path.StartsWithSegments("/Auth", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        ctx.Response.Headers.Pragma = "no-cache";
        ctx.Response.Headers.Expires = "0";
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// WebSockets Support (اگر realtime/SignalR داری)
app.UseWebSockets();

app.UseRouting();

// Authentication/Authorization برای MVC UI
app.UseAuthentication();
app.UseAuthorization();

// YARP Proxy: /api/* → Backend
// این باید بعد از UseRouting و قبل از MapControllers باشد
app.MapReverseProxy();

// MVC Routes
app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

// Health Check برای خود MVC (اختیاری)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "MVC",
    timestamp = DateTime.UtcNow
}))
.AllowAnonymous();

app.Run();
