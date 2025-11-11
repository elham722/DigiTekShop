namespace DigiTekShop.MVC.Services;
public interface ITokenStore
{
    string? GetAccessToken();
    string? GetRefreshToken();
    Task UpdateTokensAsync(string newAccessToken, DateTimeOffset? accessTokenExpiresAt, string? refreshToken, DateTimeOffset? refreshTokenExpiresAt, CancellationToken ct);
    Task UpdateAccessTokenAsync(string newAccessToken, DateTimeOffset? expiresAt, CancellationToken ct);
    Task OnRefreshFailedAsync(CancellationToken ct); // signout یا invalidation
}

