using System.Net.Http.Headers;
using DigiTekShop.MVC.Services;
using Microsoft.Extensions.Options;

namespace DigiTekShop.MVC.Handlers;

internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _store;
    private readonly Uri _apiBase;

    public BearerTokenHandler(ITokenStore store, IOptions<ApiClientOptions> opt)
    {
        _store = store;
        _apiBase = new Uri(opt.Value.BaseAddress);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        var isRelative = !uri.IsAbsoluteUri;
        var targetIsApi = isRelative || (
            uri.Scheme.Equals(_apiBase.Scheme, StringComparison.OrdinalIgnoreCase) &&
            uri.Host.Equals(_apiBase.Host, StringComparison.OrdinalIgnoreCase) &&
            uri.Port == _apiBase.Port);

        if (targetIsApi && request.Headers.Authorization is null)
        {
            var token = _store.GetAccessToken();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, ct);
    }
}
