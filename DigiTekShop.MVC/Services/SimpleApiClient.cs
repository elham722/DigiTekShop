using DigiTekShop.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DigiTekShop.MVC.Services;
public sealed class SimpleApiClient : ISimpleApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SimpleApiClient> _logger;
    private readonly IHttpContextAccessor _httpContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public SimpleApiClient(HttpClient http, ILogger<SimpleApiClient> logger, IHttpContextAccessor accessor)
    {
        _http = http;
        _logger = logger;
        _httpContext = accessor;
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, string? accessToken = null, CancellationToken ct = default)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, endpoint, accessToken);
            req.Content = CreateJsonContent(request);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                await LogApiError(endpoint, resp);
                return default;
            }

            return await ReadJson<TResponse>(resp);
        }
        catch (TaskCanceledException) { _logger.LogWarning("Timeout/Cancelled at POST {Endpoint}", endpoint); return default; }
        catch (Exception ex) { _logger.LogError(ex, "POST failed at {Endpoint}", endpoint); return default; }
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, string? accessToken = null, CancellationToken ct = default)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, endpoint, accessToken);
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                await LogApiError(endpoint, resp);
                return default;
            }
            return await ReadJson<TResponse>(resp);
        }
        catch (TaskCanceledException) { _logger.LogWarning("Timeout/Cancelled at GET {Endpoint}", endpoint); return default; }
        catch (Exception ex) { _logger.LogError(ex, "GET failed at {Endpoint}", endpoint); return default; }
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest request, string? accessToken = null, CancellationToken ct = default)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, endpoint, accessToken);
            req.Content = CreateJsonContent(request);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                await LogApiError(endpoint, resp);
                return false;
            }
            return true;
        }
        catch (TaskCanceledException) { _logger.LogWarning("Timeout/Cancelled at POST {Endpoint}", endpoint); return false; }
        catch (Exception ex) { _logger.LogError(ex, "POST failed at {Endpoint}", endpoint); return false; }
    }

    public async Task<bool> PostMultipartAsync(string endpoint, IDictionary<string, string>? fields, IEnumerable<(string Name, string FileName, Stream Stream)> files, string? accessToken = null, CancellationToken ct = default)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, endpoint, accessToken);

            using var form = new MultipartFormDataContent();
            if (fields is not null)
                foreach (var kv in fields)
                    form.Add(new StringContent(kv.Value ?? string.Empty, Encoding.UTF8), kv.Key);

            foreach (var (name, fileName, stream) in files)
            {
                var sc = new StreamContent(stream);
                sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(sc, name, fileName);
            }

            req.Content = form;

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                await LogApiError(endpoint, resp);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multipart POST failed at {Endpoint}", endpoint);
            return false;
        }
    }

    #region Helpers

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, string? accessToken)
    {
        var uri = Uri.TryCreate(endpoint, UriKind.Absolute, out var abs)
            ? abs
            : new Uri(endpoint.TrimStart('/'), UriKind.Relative);

        var req = new HttpRequestMessage(method, uri);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!req.Headers.UserAgent.Any())
            req.Headers.UserAgent.ParseAdd("DigiTekShop.MVC/1.0");

        var token = !string.IsNullOrWhiteSpace(accessToken)
            ? accessToken
            : _httpContext.HttpContext?.User?.FindFirst("access_token")?.Value;

        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var did = GetOrCreateDeviceId();
        req.Headers.TryAddWithoutValidation("X-Device-Id", did);
        req.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString("N"));

        return req;
    }

    private static StringContent CreateJsonContent<T>(T data)
        => new(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json");

    private async Task<T?> ReadJson<T>(HttpResponseMessage resp)
        => await JsonSerializer.DeserializeAsync<T>(await resp.Content.ReadAsStreamAsync(), JsonOptions);

    private string GetOrCreateDeviceId()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null) return Guid.NewGuid().ToString("N");

        if (ctx.Request.Cookies.TryGetValue("did", out var existing) && !string.IsNullOrWhiteSpace(existing))
            return existing;

        var value = Guid.NewGuid().ToString("N");
        ctx.Response.Cookies.Append("did", value, new CookieOptions
        {
            HttpOnly = false,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/"
        });
        return value;
    }

    private async Task LogApiError(string endpoint, HttpResponseMessage resp)
    {
        string body = string.Empty;
        try { body = await resp.Content.ReadAsStringAsync(); } catch { /* ignore */ }


        string? message = null;
        try
        {
            var pd = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
            message = pd?.Detail ?? pd?.Title;
        }
        catch { }

        _logger.LogWarning("API Error at {Endpoint} [{Status}] => {Message} | Raw: {Body}",
            endpoint, (int)resp.StatusCode, message ?? "(no message)", Truncate(body, 1500));
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);

    #endregion


}
