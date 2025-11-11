using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using DigiTekShop.Contracts.DTOs.Auth.Token;
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

            // گرفتن RefreshToken از Cookie
            var refreshToken = _store.GetRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await _store.OnRefreshFailedAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // ارسال درخواست Refresh
            var refreshClient = _factory.CreateClient("ApiRaw");
            var refreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
            var json = JsonSerializer.Serialize(refreshRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var refreshResp = await refreshClient.PostAsync(ApiRoutes.Auth.Refresh, content, ct);
            if (!refreshResp.IsSuccessStatusCode)
            {
                await _store.OnRefreshFailedAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // خواندن پاسخ - API از ApiResponse<T> استفاده می‌کند
            var apiEnvelope = await refreshResp.Content.ReadFromJsonAsync<ApiEnvelope<RefreshTokenResponse>>(cancellationToken: ct);
            if (apiEnvelope?.Data is null || string.IsNullOrWhiteSpace(apiEnvelope.Data.AccessToken))
            {
                await _store.OnRefreshFailedAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var payload = apiEnvelope.Data;

            // به‌روزرسانی توکن‌ها (AccessToken و RefreshToken جدید)
            // RefreshTokenExpiresAt از ExpiresAtUtc محاسبه می‌شود (30 روز از الان)
            var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
            await _store.UpdateTokensAsync(
                payload.AccessToken, 
                payload.ExpiresAtUtc, 
                payload.RefreshToken, 
                refreshTokenExpiresAt,
                ct);

            var retryReq = await request.CloneAsync(ct);
          
            return await base.SendAsync(retryReq, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record ApiEnvelope<T>(T? Data, string? TraceId, DateTimeOffset? Timestamp);
}
