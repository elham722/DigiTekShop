var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
    o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});



builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDataProtection()
    .SetApplicationName("DigiTekShop.MVC")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// ⚠️ ARCHITECTURE NOTE:
// ما دیگر CookieAuthentication جداگانه نداریم.
// احراز هویت فقط از طریق JWT در Backend API انجام می‌شود.
// MVC فقط کوکی‌های dt_at و dt_rt را نگه‌داری می‌کند و YARP آن‌ها را به Bearer Header تبدیل می‌کند.

builder.Services.AddAuthorization();


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

// Response Compression (فقط Production - برای جلوگیری از تداخل با Browser Link در Development)
if (!builder.Environment.IsDevelopment())
{
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
}


builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});


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
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms(transformBuilderContext =>
    {
        // اضافه کردن TraceId برای observability
        transformBuilderContext.AddRequestHeader("X-Request-ID", "{TraceIdentifier}", append: false);

        // 🔑 تبدیل کوکی dt_at به Authorization Bearer Header (فقط برای /api/*)
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            var httpContext = transformContext.HttpContext;

            // فقط برای مسیرهای /api
            if (httpContext.Request.Path.StartsWithSegments("/api"))
            {
                // خواندن AccessToken از کوکی
                if (httpContext.Request.Cookies.TryGetValue("dt_at", out var accessToken) 
                    && !string.IsNullOrWhiteSpace(accessToken))
                {
                    // ست کردن Bearer Token در هدر درخواست به Backend API
                    transformContext.ProxyRequest.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }
            }

            return default; // ValueTask<TResult> برای synchronous transform
        });
    });


var app = builder.Build();


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
   
    ForwardLimit = 1 
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Response Compression (فقط Production)
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

// Security Headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
 
    if (!app.Environment.IsDevelopment())
    {
        headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + 
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'";
    }
    
    await next();
});


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


app.UseWebSockets();

app.UseRouting();

// ⚠️ UseAuthentication حذف شد چون دیگر CookieAuth Scheme نداریم
// احراز هویت در Backend API انجام می‌شود؛ YARP کوکی را به Bearer Header تبدیل می‌کند
app.UseAuthorization();


app.MapReverseProxy();


app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "MVC",
    timestamp = DateTime.UtcNow
}))
.AllowAnonymous();

app.Run();
