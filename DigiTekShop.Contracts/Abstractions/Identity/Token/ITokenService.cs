namespace DigiTekShop.Contracts.Abstractions.Identity.Token;

public interface ITokenService
{
    Task<Result<RefreshTokenResponse>> IssueAsync(Guid userId, CancellationToken ct = default); 
    Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest dto, CancellationToken ct);
    Task<Result> RevokeAsync(string? refreshToken, Guid userId, CancellationToken ct); 
    Task<Result> RevokeAllAsync(Guid userId, CancellationToken ct);
    Task<Result> RevokeAccessJtiAsync(string jti, CancellationToken ct);
}


