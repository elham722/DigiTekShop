using DigiTekShop.Contracts.Abstractions.Identity.Auth;

namespace DigiTekShop.Application.Auth.LoginOrRegister.Commands;
public sealed class SendOtpCommandHandler : ICommandHandler<SendOtpCommand>
{
    private readonly IAuthService _auth;
    public SendOtpCommandHandler(IAuthService auth) => _auth = auth;

    public Task<Result> Handle(SendOtpCommand request, CancellationToken ct)
        => _auth.SendOtpAsync(request.Dto, ct);
}
