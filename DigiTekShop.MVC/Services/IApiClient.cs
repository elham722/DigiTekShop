namespace DigiTekShop.MVC.Services;

public interface IApiClient
{
    Task<ApiResult<TResponse>> GetAsync<TResponse>(string path, CancellationToken ct = default);
    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiResult<Unit>> PostAsync<TRequest>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiResult<Unit>> DeleteAsync(string path, CancellationToken ct = default);
    Task<ApiResult<Unit>> PostMultipartAsync(string path, IDictionary<string, string>? fields, IEnumerable<FormFilePart> files, CancellationToken ct = default);
}