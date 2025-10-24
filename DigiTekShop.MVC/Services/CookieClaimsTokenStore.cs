using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DigiTekShop.MVC.Services;

public sealed class CookieClaimsTokenStore : ITokenStore
{
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<CookieClaimsTokenStore> _logger;

    public CookieClaimsTokenStore(IHttpContextAccessor ctx, ILogger<CookieClaimsTokenStore> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public string? GetAccessToken()
    {
        var user = _ctx.HttpContext?.User;
        return user?.FindFirst("access_token")?.Value;
    }

    public async Task UpdateAccessTokenAsync(string newAccessToken, DateTimeOffset? expiresAt, CancellationToken ct)
    {
        var http = _ctx.HttpContext ?? throw new InvalidOperationException("No HttpContext");
        var auth = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded || auth.Principal == null)
        {
            _logger.LogWarning("Cannot update access token: no authenticated principal");
            return;
        }

        var identity = (ClaimsIdentity)auth.Principal.Identity!;
        // حذف Claim قبلی
        var old = identity.FindFirst("access_token");
        if (old is not null) identity.RemoveClaim(old);
        // افزودن Claim جدید
        identity.AddClaim(new Claim("access_token", newAccessToken));

        // می‌تونی زمان انقضا را هم به عنوان claim یا در Properties ذخیره کنی
        var props = auth.Properties ?? new AuthenticationProperties();
        if (expiresAt.HasValue)
        {
            props.ExpiresUtc = expiresAt;             // برای SlidingExpiration
            props.IsPersistent = true;                // اگر می‌خواهی بماند
            props.AllowRefresh = true;
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);
    }

    public async Task OnRefreshFailedAsync(CancellationToken ct)
    {
        var http = _ctx.HttpContext;
        if (http is null) return;

        // ساین‌اوت؛ می‌تونی به صفحهٔ لاگین هم ریدایرکت کنی
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
