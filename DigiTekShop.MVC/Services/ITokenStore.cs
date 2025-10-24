namespace DigiTekShop.MVC.Services;
public interface ITokenStore
{
    string? GetAccessToken();
    Task UpdateAccessTokenAsync(string newAccessToken, DateTimeOffset? expiresAt, CancellationToken ct);
    Task OnRefreshFailedAsync(CancellationToken ct); // signout یا invalidation
}

