using System.Diagnostics;

namespace DigiTekShop.MVC.Handlers;

internal sealed class CorrelationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public CorrelationHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var http = _ctx.HttpContext;

        var cid = http?.TraceIdentifier ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        if (!request.Headers.Contains("X-Request-Id"))
            request.Headers.TryAddWithoutValidation("X-Request-Id", cid);

        var did = http?.Request.Cookies["did"];
        if (!string.IsNullOrWhiteSpace(did))
            request.Headers.TryAddWithoutValidation("X-Device-Id", did);

        var lang = http?.Request.Headers["Accept-Language"].ToString();
        if (!string.IsNullOrWhiteSpace(lang))
            request.Headers.TryAddWithoutValidation("Accept-Language", lang);

        return base.SendAsync(request, ct);
    }
}