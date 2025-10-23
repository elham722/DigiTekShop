using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Command;
public sealed record VerifyOtpCommand(VerifyOtpRequestDto Dto)
    : ICommand<LoginResponseDto>, INonTransactionalCommand;