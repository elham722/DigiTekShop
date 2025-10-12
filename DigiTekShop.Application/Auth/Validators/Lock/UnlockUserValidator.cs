using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators.Lock
{
    public sealed class UnlockUserValidator : AbstractValidator<UnlockUserRequestDto>
    {
        public UnlockUserValidator() => RuleFor(x => x.UserId).NotEmpty();
    }
}
