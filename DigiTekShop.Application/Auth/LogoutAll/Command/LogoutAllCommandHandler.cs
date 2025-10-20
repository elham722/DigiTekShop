using DigiTekShop.Contracts.Abstractions.Identity.Auth;

namespace DigiTekShop.Application.Auth.LogoutAll.Command;
public sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly ILogoutService _svc;
    public LogoutAllCommandHandler(ILogoutService svc) => _svc = svc;

    public Task<Result> Handle(LogoutAllCommand request, CancellationToken ct)
        => _svc.LogoutAllAsync(request.Dto, ct);
}