using DigiTekShop.Contracts.DTOs.Auth.Lockout;

namespace DigiTekShop.Application.Admin.Users.Commands.UnlockUser;

public sealed record UnlockUserCommand(Guid UserId)
    : ICommand<UnlockUserResponseDto>;

