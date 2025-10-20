using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Application.Auth.Login.Command;
public sealed record LoginCommand(LoginRequest Dto)
    : ICommand<LoginResponse>, INonTransactionalCommand;
