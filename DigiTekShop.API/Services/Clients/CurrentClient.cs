using DigiTekShop.Contracts.Abstractions.Clients;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DigiTekShop.API.Services.Clients;
public sealed class CurrentClient : ICurrentClient
{
    private readonly IHttpContextAccessor _http;
    public CurrentClient(IHttpContextAccessor http) => _http = http;

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

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(raw); 

            _raw = raw;
            _jti = jwt.Id;

            var subStr = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub
                                                     || c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(subStr, out var g)) _sub = g;

            var iatStr = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;
            if (long.TryParse(iatStr, out var iatUnix))
                _iatUtc = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;

            var expStr = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
            if (long.TryParse(expStr, out var expUnix))
                _expUtc = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            else if (jwt.ValidTo != default)
                _expUtc = jwt.ValidTo.ToUniversalTime();
        }
        catch
        {
        }
    }

    public string? AccessTokenRaw { get { EnsureParsedOnce(); return _raw; } }
    public string? AccessTokenJti { get { EnsureParsedOnce(); return _jti; } }
    public Guid? AccessTokenSubject { get { EnsureParsedOnce(); return _sub; } }
    public DateTime? AccessTokenIssuedAtUtc { get { EnsureParsedOnce(); return _iatUtc; } }
    public DateTime? AccessTokenExpiresAtUtc { get { EnsureParsedOnce(); return _expUtc; } }

}
