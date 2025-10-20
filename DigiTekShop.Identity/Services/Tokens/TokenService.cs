using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Token;

namespace DigiTekShop.Identity.Services.Tokens;

public class TokenService : ITokenService
{
    public Task<Result<RefreshTokenResponse>> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<RefreshTokenResponse>> RefreshAsync(RefreshTokenRequest dto, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> RevokeAccessJtiAsync(string jti, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> RevokeAsync(string? refreshToken, Guid userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}

