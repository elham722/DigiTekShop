using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DigiTekShop.MVC.Services;

internal sealed class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ApiResult<TResponse>> GetAsync<TResponse>(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        Prepare(req);
        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<TResponse>(path, resp);
        }
        catch (TaskCanceledException tce)
        {
            return FailFromCancel<TResponse>(path, tce);
        }
        catch (HttpRequestException hre)
        {
            return FailFromHttp<TResponse>(path, hre);
        }
        catch (Exception ex)
        {
            return FailFromUnknown<TResponse>(path, ex);
        }
    }

    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        Prepare(req);
        req.Content = CreateJson(body);
        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<TResponse>(path, resp);
        }
        catch (TaskCanceledException tce) { return FailFromCancel<TResponse>(path, tce); }
        catch (HttpRequestException hre) { return FailFromHttp<TResponse>(path, hre); }
        catch (Exception ex) { return FailFromUnknown<TResponse>(path, ex); }
    }

    public async Task<ApiResult<Unit>> PostAsync<TRequest>(string path, TRequest body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        Prepare(req);
        req.Content = CreateJson(body);
        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<Unit>(path, resp);
        }
        catch (TaskCanceledException tce) { return FailFromCancel<Unit>(path, tce); }
        catch (HttpRequestException hre) { return FailFromHttp<Unit>(path, hre); }
        catch (Exception ex) { return FailFromUnknown<Unit>(path, ex); }
    }

    public async Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, path);
        Prepare(req);
        req.Content = CreateJson(body);
        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<TResponse>(path, resp);
        }
        catch (TaskCanceledException tce) { return FailFromCancel<TResponse>(path, tce); }
        catch (HttpRequestException hre) { return FailFromHttp<TResponse>(path, hre); }
        catch (Exception ex) { return FailFromUnknown<TResponse>(path, ex); }
    }

    public async Task<ApiResult<Unit>> DeleteAsync(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, path);
        Prepare(req);
        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<Unit>(path, resp);
        }
        catch (TaskCanceledException tce) { return FailFromCancel<Unit>(path, tce); }
        catch (HttpRequestException hre) { return FailFromHttp<Unit>(path, hre); }
        catch (Exception ex) { return FailFromUnknown<Unit>(path, ex); }
    }

    public async Task<ApiResult<Unit>> PostMultipartAsync(string path, IDictionary<string, string>? fields, IEnumerable<FormFilePart> files, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        Prepare(req);

        using var form = new MultipartFormDataContent();
        if (fields is not null)
            foreach (var (k, v) in fields)
                form.Add(new StringContent(v ?? string.Empty, Encoding.UTF8), k);

        foreach (var part in files)
        {
            var sc = new StreamContent(part.Content);
            sc.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(part.ContentType) ? "application/octet-stream" : part.ContentType);
            form.Add(sc, part.Name, part.FileName);
        }
        req.Content = form;

        try
        {
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            return await Read<Unit>(path, resp);
        }
        catch (TaskCanceledException tce) { return FailFromCancel<Unit>(path, tce); }
        catch (HttpRequestException hre) { return FailFromHttp<Unit>(path, hre); }
        catch (Exception ex) { return FailFromUnknown<Unit>(path, ex); }
    }

    #region Helpers

    private static StringContent CreateJson<T>(T data)
        => new(JsonSerializer.Serialize(data, Json), Encoding.UTF8, "application/json");

    private void Prepare(HttpRequestMessage req)
    {
        if (!req.Headers.Accept.Any())
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!req.Headers.UserAgent.Any())
            req.Headers.UserAgent.ParseAdd("DigiTekShop.MVC/1.0");
    }

    private async Task<ApiResult<T>> Read<T>(string path, HttpResponseMessage resp)
    {
        var code = resp.StatusCode;
        var status = (int)code;

        // 204/205
        if (code is HttpStatusCode.NoContent or HttpStatusCode.ResetContent)
        {
            return ApiResult<T>.Ok(typeof(T) == typeof(Unit) ? (T)(object)Unit.Value : default!, code);
        }


        var media = resp.Content?.Headers.ContentType?.MediaType ?? "";
        var isJson = media.Contains("json", StringComparison.OrdinalIgnoreCase)
                     || string.IsNullOrWhiteSpace(media);

        if (resp.IsSuccessStatusCode)
        {
            if (!isJson)
                return ApiResult<T>.Fail(new ProblemDetails { Title = "Unexpected content-type", Status = status, Detail = media }, code);

            try
            {
                var env = await resp.Content!.ReadFromJsonAsync<ApiEnvelope<T>>(Json);
                if (env is not null)
                    return ApiResult<T>.Ok(env.Data!, code);
            }
            catch
            {
                // ignore
            }

            try
            {
                var model = await resp.Content!.ReadFromJsonAsync<T>(Json);
                return ApiResult<T>.Ok(model!, code);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize success response from {Path} into ApiEnvelope<{Type}> or {Type}", path, typeof(T).Name);
                return ApiResult<T>.Fail(new ProblemDetails { Title = "Invalid JSON", Status = status, Detail = "Deserialization failed" }, code);
            }
        }

        if (isJson)
        {
            try
            {
                var pd = await resp.Content!.ReadFromJsonAsync<ProblemDetails>(Json);
                return ApiResult<T>.Fail(pd, code);
            }
            catch { /* fall-through */ }
        }

        var raw = (await resp.Content!.ReadAsStringAsync()) ?? string.Empty;
        var problem = new ProblemDetails { Title = "API Error", Status = status, Detail = Truncate(raw, 600) };
        _logger.LogWarning("API {Status} at {Path}: {Title} | {Detail}", status, path, problem.Title, Truncate(problem.Detail, 600));
        return ApiResult<T>.Fail(problem, code);
    }


    private ApiResult<T> FailFromCancel<T>(string path, TaskCanceledException tce)
    {
        var pd = new ProblemDetails
        {
            Title = "Request cancelled or timed out",
            Detail = tce.InnerException?.Message ?? tce.Message,
            Status = StatusCodes.Status499ClientClosedRequest
        };
        _logger.LogWarning(tce, "HTTP cancelled at {Path}", path);
        return ApiResult<T>.Fail(pd, (HttpStatusCode)pd.Status!.Value);
    }

    private ApiResult<T> FailFromHttp<T>(string path, HttpRequestException hre)
    {
        var status = hre.StatusCode.HasValue ? (int)hre.StatusCode.Value : StatusCodes.Status502BadGateway;
        var pd = new ProblemDetails
        {
            Title = "HTTP request failed",
            Detail = hre.Message,
            Status = status
        };
        _logger.LogWarning(hre, "HTTP error at {Path}", path);
        return ApiResult<T>.Fail(pd, hre.StatusCode ?? HttpStatusCode.BadGateway);
    }

    private ApiResult<T> FailFromUnknown<T>(string path, Exception ex)
    {
        var pd = new ProblemDetails
        {
            Title = "Unexpected error",
            Detail = ex.Message,
            Status = StatusCodes.Status500InternalServerError
        };
        _logger.LogError(ex, "Unexpected error at {Path}", path);
        return ApiResult<T>.Fail(pd, HttpStatusCode.InternalServerError);
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    #endregion
}
