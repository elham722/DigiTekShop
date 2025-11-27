using DigiTekShop.Contracts.Abstractions.Identity.Lockout;
using DigiTekShop.Contracts.Abstractions.Identity.Security;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;

namespace DigiTekShop.Application.Admin.Users.Commands.LockUser;

public sealed class LockUserCommandHandler
    : ICommandHandler<LockUserCommand, LockUserResponseDto>
{
    private readonly ILockoutService _lockoutService;

    public LockUserCommandHandler(ILockoutService lockoutService)
    {
        _lockoutService = lockoutService;
    }

    public async Task<Result<LockUserResponseDto>> Handle(
        LockUserCommand request,
        CancellationToken ct)
    {
        var dto = new LockUserRequestDto(request.UserId, LockoutEnd: null);
        return await _lockoutService.LockUserAsync(dto, ct);
    }
}

