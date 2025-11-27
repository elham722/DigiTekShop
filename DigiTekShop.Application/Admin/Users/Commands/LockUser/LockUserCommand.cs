using DigiTekShop.Contracts.DTOs.Auth.Lockout;

namespace DigiTekShop.Application.Admin.Users.Commands.LockUser;

public sealed record LockUserCommand(Guid UserId)
    : ICommand<LockUserResponseDto>;

