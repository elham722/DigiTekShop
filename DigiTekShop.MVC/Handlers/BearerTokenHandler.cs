using System.Net.Http.Headers;
using DigiTekShop.MVC.Services;
using Microsoft.Extensions.Options;

namespace DigiTekShop.MVC.Handlers;

internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    private readonly Uri _apiBase;

    public BearerTokenHandler(IHttpContextAccessor ctx, IOptions<ApiClientOptions> opt)
    {
        _ctx = ctx;
        _apiBase = new Uri(opt.Value.BaseAddress);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        var targetIsApi =
            !uri.IsAbsoluteUri ||
            (uri.Host.Equals(_apiBase.Host, StringComparison.OrdinalIgnoreCase) &&
             uri.Scheme.Equals(_apiBase.Scheme, StringComparison.OrdinalIgnoreCase));

        if (targetIsApi && request.Headers.Authorization is null)
        {
            var http = _ctx.HttpContext;
            var token = http?.User?.FindFirst("access_token")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, ct);
    }
}