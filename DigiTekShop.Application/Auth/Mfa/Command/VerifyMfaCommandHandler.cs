using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Application.Auth.Mfa.Command;
public sealed class VerifyMfaCommandHandler
    : ICommandHandler<VerifyMfaCommand,LoginResponse>
{
    private readonly IMfaService _svc;
    public VerifyMfaCommandHandler(IMfaService svc) => _svc = svc;

    public Task<Result<LoginResponse>> Handle(VerifyMfaCommand request, CancellationToken ct)
        => _svc.VerifyAsync(request.Dto, ct);
}