using DigiTekShop.API.Common.Http;
using System.Text.RegularExpressions;

namespace DigiTekShop.API.Middleware;

public sealed class ClientContextMiddleware(RequestDelegate next, ILogger<ClientContextMiddleware> logger)
{
    private static readonly Regex UuidV4Regex =
        new("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$",
            RegexOptions.Compiled);

    public async Task Invoke(HttpContext ctx)
    {
        string? deviceId = null;

        if (ctx.Request.Headers.TryGetValue(HeaderNames.DeviceId, out var devHeader) && !string.IsNullOrWhiteSpace(devHeader))
        {
            var candidate = devHeader.ToString();
            if (UuidV4Regex.IsMatch(candidate))
                deviceId = candidate;
        }

        if (deviceId is null && ctx.Request.Cookies.TryGetValue(CookieNames.DeviceId, out var devCookie))
        {
            if (UuidV4Regex.IsMatch(devCookie))
                deviceId = devCookie;
        }

        if (deviceId is null)
        {
            deviceId = Guid.NewGuid().ToString(); 
            ctx.Response.Cookies.Append(CookieNames.DeviceId, deviceId, new CookieOptions
            {
                HttpOnly = false,       
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            
            ctx.Response.Headers[HeaderNames.DeviceId] = deviceId;
        }

        ctx.Items["DeviceId"] = deviceId;

        
        string? ua = null;
        if (ctx.Request.Headers.TryGetValue(HeaderNames.UserAgent, out var uaHeader) && !string.IsNullOrWhiteSpace(uaHeader))
            ua = uaHeader.ToString();
        ctx.Items["UserAgent"] = ua;

        
        string? ip = null;
        if (ctx.Request.Headers.TryGetValue(HeaderNames.ForwardedFor, out var fwd) && !string.IsNullOrWhiteSpace(fwd))
        {
            ip = fwd.ToString().Split(',')[0].Trim(); 
        }
        else if (ctx.Request.Headers.TryGetValue(HeaderNames.RealIp, out var real) && !string.IsNullOrWhiteSpace(real))
        {
            ip = real.ToString();
        }
        else
        {
            ip = ctx.Connection.RemoteIpAddress?.ToString();
        }
        ctx.Items["IpAddress"] = ip;

        await next(ctx);
    }
}
