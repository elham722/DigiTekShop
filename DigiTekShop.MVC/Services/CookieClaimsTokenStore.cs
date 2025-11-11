using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DigiTekShop.MVC.Services;

public sealed class CookieClaimsTokenStore : ITokenStore
{
    private const string RefreshTokenCookieName = "rt";
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<CookieClaimsTokenStore> _logger;
    private readonly IWebHostEnvironment _env;

    public CookieClaimsTokenStore(
        IHttpContextAccessor ctx, 
        ILogger<CookieClaimsTokenStore> logger,
        IWebHostEnvironment env)
    {
        _ctx = ctx;
        _logger = logger;
        _env = env;
    }

    public string? GetAccessToken()
    {
        var user = _ctx.HttpContext?.User;
        return user?.FindFirst("access_token")?.Value;
    }

    public string? GetRefreshToken()
    {
        var http = _ctx.HttpContext;
        if (http is null) return null;
        return http.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rt) ? rt : null;
    }

    public async Task UpdateTokensAsync(
        string newAccessToken, 
        DateTimeOffset? accessTokenExpiresAt, 
        string? refreshToken, 
        DateTimeOffset? refreshTokenExpiresAt, 
        CancellationToken ct)
    {
        var http = _ctx.HttpContext ?? throw new InvalidOperationException("No HttpContext");
        var auth = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded || auth.Principal == null)
        {
            _logger.LogWarning("Cannot update tokens: no authenticated principal");
            return;
        }

        var identity = (ClaimsIdentity)auth.Principal.Identity!;
        // حذف Claim قبلی
        var old = identity.FindFirst("access_token");
        if (old is not null) identity.RemoveClaim(old);
        // افزودن Claim جدید
        identity.AddClaim(new Claim("access_token", newAccessToken));

        var props = auth.Properties ?? new AuthenticationProperties();
        if (accessTokenExpiresAt.HasValue)
        {
            props.ExpiresUtc = accessTokenExpiresAt;
            props.IsPersistent = true;
            props.AllowRefresh = true;
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);

        // ذخیره RefreshToken در HttpOnly Cookie
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _env.IsDevelopment() ? false : true, // در Development می‌تواند false باشد
                SameSite = SameSiteMode.Lax,
                Expires = refreshTokenExpiresAt ?? DateTimeOffset.UtcNow.AddDays(30)
            };
            http.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
        }
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

        var props = auth.Properties ?? new AuthenticationProperties();
        if (expiresAt.HasValue)
        {
            props.ExpiresUtc = expiresAt;
            props.IsPersistent = true;
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

        // پاک کردن RefreshToken Cookie
        http.Response.Cookies.Delete(RefreshTokenCookieName);
        
        // ساین‌اوت
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
