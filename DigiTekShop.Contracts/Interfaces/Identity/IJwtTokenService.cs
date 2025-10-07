using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.SharedKernel.Results;
using System.Security.Claims;

namespace DigiTekShop.Contracts.Interfaces.Identity;
public interface IJwtTokenService
{
    Task<Result<TokenResponseDto>> GenerateTokensAsync(string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> RefreshTokensAsync(string refreshToken, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, CancellationToken ct = default);
    Task<Result> RevokeAllUserTokensAsync(string userId, string? reason = null, CancellationToken ct = default);
    Task<Result<ClaimsPrincipal>> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
    Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId, CancellationToken ct = default);
    Task CleanupExpiredTokensAsync(CancellationToken ct = default);
}


