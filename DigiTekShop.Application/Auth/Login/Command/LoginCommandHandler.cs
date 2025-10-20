using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Application.Auth.Login.Command;
public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly ILoginService _svc;
    public LoginCommandHandler(ILoginService svc) => _svc = svc;

    public Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
        => _svc.LoginAsync(request.Dto, ct);
}
