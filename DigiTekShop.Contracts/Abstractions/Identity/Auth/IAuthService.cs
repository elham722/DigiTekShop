using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.Contracts.Abstractions.Identity.Auth;
public interface IAuthService
{
    Task<Result> SendOtpAsync(SendOtpRequestDto dto, CancellationToken ct);
    Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto dto, CancellationToken ct);
}
