using DigiTekShop.Contracts.Abstractions.Identity.Auth;

namespace DigiTekShop.Application.Auth.Logout.Command;
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly ILogoutService _svc;
    public LogoutCommandHandler(ILogoutService svc) => _svc = svc;

    public Task<Result> Handle(LogoutCommand request, CancellationToken ct)
        => _svc.LogoutAsync(request.Dto, ct);
}