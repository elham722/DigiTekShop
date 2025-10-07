using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Interfaces.Identity;

public interface IAuthService
{
    Task<Result<TokenResponseDto>> RegisterAsync(RegisterRequestDto req, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto req, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto req, CancellationToken ct = default);
    Task<Result> RevokeAsync(RevokeRequestDto req, CancellationToken ct = default);
}