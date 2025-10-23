using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.LoginOrRegister;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Command;
public sealed class VerifyOtpCommandHandler : ICommandHandler<VerifyOtpCommand, LoginResponseDto>
{
    private readonly IAuthService _auth;
    public VerifyOtpCommandHandler(IAuthService auth) => _auth = auth;

    public Task<Result<LoginResponseDto>> Handle(VerifyOtpCommand request, CancellationToken ct)
        => _auth.VerifyOtpAsync(request.Dto, ct);
}
