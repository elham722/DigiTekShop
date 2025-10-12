using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Contracts.Abstractions.CQRS.Commands;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Application.Auth.Register.Command
{
    public sealed record RegisterUserCommand(RegisterRequestDto Dto)
        : ICommand<RegisterResponseDto>;

}
