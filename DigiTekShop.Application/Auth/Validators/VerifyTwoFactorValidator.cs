using DigiTekShop.Contracts.DTOs.Auth.TwoFactor;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators
{
    public sealed class VerifyTwoFactorValidator : AbstractValidator<VerifyTwoFactorRequestDto>
    {
        public VerifyTwoFactorValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty().Length(4, 10);
        }
    }
}
