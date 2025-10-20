using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.DTOs.Auth.Token;

namespace DigiTekShop.Application.Auth.Tokens.Command;
public sealed class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand,RefreshTokenResponse>
{
    private readonly ITokenService _svc;
    public RefreshTokenCommandHandler(ITokenService svc) => _svc = svc;

    public Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
        => _svc.RefreshAsync(request.Dto, ct);
}