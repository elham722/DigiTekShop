using DigiTekShop.Contracts.Auth.TwoFactor;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators
{
    public sealed class TwoFactorRequestValidator : AbstractValidator<TwoFactorRequestDto>
    {
        public TwoFactorRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Provider).IsInEnum();
        }
    }
}
