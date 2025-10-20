using DigiTekShop.Contracts.DTOs.Auth.Logout;

namespace DigiTekShop.Application.Auth.LogoutAll.Command;
public sealed record LogoutAllCommand(LogoutAllRequest Dto)
    : ICommand, INonTransactionalCommand;