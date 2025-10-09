using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Auth.Register.Command
{
    public sealed class RegisterUserCommandHandler
        : IRequestHandler<RegisterUserCommand, Result<RegisterResponseDto>>
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
