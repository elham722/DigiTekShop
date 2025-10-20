using DigiTekShop.Contracts.Abstractions.Clients;

namespace DigiTekShop.API.Services.Clients;
public sealed class CurrentClient : ICurrentClient
{
    private readonly IHttpContextAccessor _http;

    public CurrentClient(IHttpContextAccessor http) => _http = http;

    public string? IpAddress
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return null;
            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent
        => _http.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? DeviceId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx is null) return null;

            var h = ctx.Request.Headers["X-Device-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(h)) return h;

            if (ctx.Request.Cookies.TryGetValue("did", out var c) && !string.IsNullOrWhiteSpace(c))
                return c;

            var q = ctx.Request.Query["did"].ToString();
            return string.IsNullOrWhiteSpace(q) ? null : q;
        }
    }
}
