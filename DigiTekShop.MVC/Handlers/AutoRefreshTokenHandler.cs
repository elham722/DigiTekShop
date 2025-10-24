using System.Net;
using System.Net.Http.Json;
using System.Threading;
using DigiTekShop.MVC.Extensions;   
using DigiTekShop.MVC.Services;    
using Microsoft.Extensions.Http;   

namespace DigiTekShop.MVC.Handlers;

internal sealed class AutoRefreshTokenHandler : DelegatingHandler
{
    
    private static readonly HttpRequestOptionsKey<bool> SkipAutoRefreshKey = new("SkipAutoRefresh");

    private readonly ITokenStore _store;
    private readonly IHttpClientFactory _factory;

  
    private static readonly SemaphoreSlim _gate = new(initialCount: 1, maxCount: 1);

    public AutoRefreshTokenHandler(ITokenStore store, IHttpClientFactory factory)
    {
        _store = store;
        _factory = factory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        
        if (request.Options.TryGetValue(SkipAutoRefreshKey, out var skip) && skip)
            return await base.SendAsync(request, ct);

        
        var resp = await base.SendAsync(request, ct);
        if (resp.StatusCode != HttpStatusCode.Unauthorized)
            return resp;

        
        resp.Dispose();

        await _gate.WaitAsync(ct);
        try
        {
           
            var probeReq = await request.CloneAsync(ct);
            probeReq.Options.Set(SkipAutoRefreshKey, true); 
            var probeResp = await base.SendAsync(probeReq, ct);
            if (probeResp.StatusCode != HttpStatusCode.Unauthorized)
                return probeResp;

            probeResp.Dispose();

            var refreshClient = _factory.CreateClient("ApiRaw"); 
            using var refreshResp = await refreshClient.PostAsync(ApiRoutes.Auth.Refresh, content: null, ct);
            if (!refreshResp.IsSuccessStatusCode)
            {
                await _store.OnRefreshFailedAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var payload = await refreshResp.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(payload?.AccessToken))
            {
                await _store.OnRefreshFailedAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            await _store.UpdateAccessTokenAsync(payload.AccessToken, payload.AccessTokenExpiresAt, ct);

            var retryReq = await request.CloneAsync(ct);
          
            return await base.SendAsync(retryReq, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record RefreshResponse(string AccessToken, DateTimeOffset? AccessTokenExpiresAt);
}
