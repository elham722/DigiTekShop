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
    .AddTransforms(builderContext =>
    {
      
        builderContext.AddRequestHeader("X-Request-ID", "{TraceIdentifier}", append: false);
       
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


app.UseAuthentication();
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
