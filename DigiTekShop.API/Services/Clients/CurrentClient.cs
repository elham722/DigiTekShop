using DigiTekShop.Contracts.Abstractions.Clients;
using DigiTekShop.Contracts.Abstractions.Identity.Token;

namespace DigiTekShop.API.Services.Clients;
public sealed class CurrentClient : ICurrentClient
{
    private readonly IHttpContextAccessor _http;
    private readonly ITokenService _tokens;

    public CurrentClient(IHttpContextAccessor http, ITokenService tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public string? IpAddress
        => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

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


    private bool _parsed;
    private string? _raw;
    private string? _jti;
    private Guid? _sub;
    private DateTime? _iatUtc;
    private DateTime? _expUtc;

    private void EnsureParsedOnce()
    {
        if (_parsed) return;
        _parsed = true;

        var ctx = _http.HttpContext;
        if (ctx is null) return;

        var auth = ctx.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var raw = auth[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        var p = _tokens.TryReadAccessToken(raw);
        if (!p.ok) return;

        _raw = raw;
        _jti = p.jti;
        _sub = p.sub;
        _iatUtc = p.iatUtc;
        _expUtc = p.expUtc;
    }

    public string? AccessTokenRaw { get { EnsureParsedOnce(); return _raw; } }
    public string? AccessTokenJti { get { EnsureParsedOnce(); return _jti; } }
    public Guid? AccessTokenSubject { get { EnsureParsedOnce(); return _sub; } }
    public DateTime? AccessTokenIssuedAtUtc { get { EnsureParsedOnce(); return _iatUtc; } }
    public DateTime? AccessTokenExpiresAtUtc { get { EnsureParsedOnce(); return _expUtc; } }

}
