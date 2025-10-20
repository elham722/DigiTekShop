using DigiTekShop.Contracts.Abstractions.Identity.EmailConfirmation;

namespace DigiTekShop.Application.Auth.ResendEmailConfirmation.Command;

public sealed class ResendEmailConfirmationCommandHandler : ICommandHandler<ResendEmailConfirmationCommand>
{
    private readonly IEmailConfirmationService _svc;
    public ResendEmailConfirmationCommandHandler(IEmailConfirmationService svc) => _svc = svc;

    public Task<Result> Handle(ResendEmailConfirmationCommand request, CancellationToken ct)
        => _svc.ResendAsync(request.Dto, ct);
}