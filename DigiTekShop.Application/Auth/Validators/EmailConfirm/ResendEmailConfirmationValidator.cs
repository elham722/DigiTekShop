using DigiTekShop.Contracts.Auth.EmailConfirmation;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators.EmailConfirm
{
    public sealed class ResendEmailConfirmationValidator : AbstractValidator<ResendEmailConfirmationRequestDto>
    {
        public ResendEmailConfirmationValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }

}
