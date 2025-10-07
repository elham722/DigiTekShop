using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.SharedKernel.Results;
using System.Security.Claims;

namespace DigiTekShop.Contracts.Interfaces.Identity;
    public interface IJwtTokenService
    {
        Task<Result<TokenResponseDto>> GenerateTokensAsync(string userId, string? deviceId = null, string? ipAddress = null, string? userAgent = null);
        Task<Result<TokenResponseDto>> RefreshTokensAsync(string refreshToken, string? deviceId = null, string? ipAddress = null, string? userAgent = null);
        Task<Result> RevokeRefreshTokenAsync(string refreshToken, string? reason = null);
        Task<Result> RevokeAllUserTokensAsync(string userId, string? reason = null);
        Task<Result<ClaimsPrincipal>> ValidateAccessTokenAsync(string accessToken);
        Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId);
        Task CleanupExpiredTokensAsync();
    }

