using DigiTekShop.Contracts.Abstractions.Identity.EmailConfirmation;

namespace DigiTekShop.Application.Auth.ConfirmEmail.Command;
public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IEmailConfirmationService _svc;
    public ConfirmEmailCommandHandler(IEmailConfirmationService svc) => _svc = svc;

    public Task<Result> Handle(ConfirmEmailCommand request, CancellationToken ct)
        => _svc.ConfirmEmailAsync(request.Dto, ct);
}