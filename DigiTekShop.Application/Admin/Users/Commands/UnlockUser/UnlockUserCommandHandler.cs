using DigiTekShop.Contracts.Abstractions.Identity.Lockout;
using DigiTekShop.Contracts.Abstractions.Identity.Security;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;

namespace DigiTekShop.Application.Admin.Users.Commands.UnlockUser;

public sealed class UnlockUserCommandHandler
    : ICommandHandler<UnlockUserCommand, UnlockUserResponseDto>
{
    private readonly ILockoutService _lockoutService;

    public UnlockUserCommandHandler(ILockoutService lockoutService)
    {
        _lockoutService = lockoutService;
    }

    public async Task<Result<UnlockUserResponseDto>> Handle(
        UnlockUserCommand request,
        CancellationToken ct)
    {
        var dto = new UnlockUserRequestDto(request.UserId);
        return await _lockoutService.UnlockUserAsync(dto, ct);
    }
}

