namespace DigiTekShop.Contracts.Interfaces.Caching;

public interface ITokenBlacklistService
{
    Task RevokeAccessTokenAsync(string jti, DateTime expiresAt, string? reason = null, CancellationToken ct = default);

    Task<bool> IsTokenRevokedAsync(string jti, CancellationToken ct = default);

    Task RevokeAllUserTokensAsync(Guid userId, string? reason = null, CancellationToken ct = default);

    Task<bool> IsUserTokensRevokedAsync(Guid userId, DateTime tokenIssuedAt, CancellationToken ct = default);
}

