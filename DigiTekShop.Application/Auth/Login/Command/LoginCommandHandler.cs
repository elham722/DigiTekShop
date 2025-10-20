using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Application.Auth.Login.Command;
public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResultDto>
{
    private readonly ILoginService _svc;
    public LoginCommandHandler(ILoginService svc) => _svc = svc;

    public Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken ct)
        => _svc.LoginAsync(request.Dto, ct);
}
