using System.Text.RegularExpressions;

namespace DigiTekShop.MVC.Middleware;

public sealed class DeviceIdMiddleware
{
    private static readonly Regex UuidV4Regex = new(
        "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$",
        RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public DeviceIdMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        const string cookieName = "did"; // DeviceId cookie name for MVC
        string? deviceId = null;

        // بررسی هدر
        if (context.Request.Headers.TryGetValue("X-Device-Id", out var devHeader) && !string.IsNullOrWhiteSpace(devHeader))
        {
            var candidate = devHeader.ToString();
            if (UuidV4Regex.IsMatch(candidate))
                deviceId = candidate;
        }

        // بررسی Cookie
        if (deviceId is null && context.Request.Cookies.TryGetValue(cookieName, out var devCookie))
        {
            if (UuidV4Regex.IsMatch(devCookie))
                deviceId = devCookie;
        }

        // اگر DeviceId وجود ندارد، یک Guid جدید بساز و در Cookie ذخیره کن
        if (deviceId is null)
        {
            deviceId = Guid.NewGuid().ToString();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // برای دسترسی JavaScript (در صورت نیاز)
                Secure = _env.IsDevelopment() ? false : true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            };
            context.Response.Cookies.Append(cookieName, deviceId, cookieOptions);
        }

        // ذخیره در Items برای استفاده در CorrelationHandler
        context.Items["DeviceId"] = deviceId;

        await _next(context);
    }
}

