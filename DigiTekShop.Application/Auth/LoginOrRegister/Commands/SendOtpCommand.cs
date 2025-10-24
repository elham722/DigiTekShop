using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Commands;
public sealed record SendOtpCommand(SendOtpRequestDto Dto)
    : ICommand, INonTransactionalCommand;
