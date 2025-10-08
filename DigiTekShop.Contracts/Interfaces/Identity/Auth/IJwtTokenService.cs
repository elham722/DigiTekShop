using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.SharedKernel.Results;
using System.Security.Claims;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth;

public interface IJwtTokenService
{
    // Token Generation & Refresh
    Task<Result<TokenResponseDto>> GenerateTokensAsync(string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> RefreshTokensAsync(string refreshToken, string? deviceId = null, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
    
    // Refresh Token Revocation
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null, CancellationToken ct = default);
    Task<Result> RevokeAllUserTokensAsync(string userId, string? reason = null, CancellationToken ct = default);
    
    // Access Token Revocation (Blacklist)
    Task<Result> RevokeAccessTokenAsync(string accessToken, string? reason = null, CancellationToken ct = default);
    
    Task<Result> RevokeAllUserAccessTokensAsync(Guid userId, string? reason = null, CancellationToken ct = default);
    
    // Token Validation
    Task<Result<ClaimsPrincipal>> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
    
    // Token Management
    Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId, CancellationToken ct = default);
    Task CleanupExpiredTokensAsync(CancellationToken ct = default);
}


