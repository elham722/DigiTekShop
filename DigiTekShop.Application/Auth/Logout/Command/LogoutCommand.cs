using DigiTekShop.Contracts.DTOs.Auth.Logout;

namespace DigiTekShop.Application.Auth.Logout.Command;
public sealed record LogoutCommand(LogoutRequest Dto)
    : ICommand, INonTransactionalCommand;