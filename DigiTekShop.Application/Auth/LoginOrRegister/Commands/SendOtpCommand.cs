using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Command;
public sealed record SendOtpCommand(SendOtpRequestDto Dto)
    : ICommand, INonTransactionalCommand;
