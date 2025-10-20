using DigiTekShop.Contracts.Abstractions.Identity.Registration;
using DigiTekShop.Contracts.DTOs.Auth.Register;

namespace DigiTekShop.Application.Auth.Register.Command
{
    public sealed class RegisterUserCommandHandler
        : ICommandHandler<RegisterUserCommand,RegisterResponseDto>
    {
        private readonly IRegistrationService _registration;

        public RegisterUserCommandHandler(IRegistrationService registration)
            => _registration = registration;

        public Task<Result<RegisterResponseDto>> Handle(
            RegisterUserCommand request,
            CancellationToken ct)
            => _registration.RegisterAsync(request.Dto, ct);
    }
}
