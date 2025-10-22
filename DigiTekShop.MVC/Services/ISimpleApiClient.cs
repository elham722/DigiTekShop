namespace DigiTekShop.MVC.Services;
public interface ISimpleApiClient
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, string? accessToken = null, CancellationToken ct = default);
    Task<TResponse?> GetAsync<TResponse>(string endpoint, string? accessToken = null, CancellationToken ct = default);
    Task<bool> PostAsync<TRequest>(string endpoint, TRequest request, string? accessToken = null, CancellationToken ct = default);
    Task<bool> PostMultipartAsync(string endpoint, IDictionary<string, string>? fields, IEnumerable<(string Name, string FileName, Stream Stream)> files, string? accessToken = null, CancellationToken ct = default);
}
