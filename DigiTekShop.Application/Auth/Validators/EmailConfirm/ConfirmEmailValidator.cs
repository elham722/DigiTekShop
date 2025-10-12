using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Contracts.Auth.EmailConfirmation;

namespace DigiTekShop.Application.Auth.Validators.EmailConfirm
{
    public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailRequestDto>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Token).NotEmpty();
        }
    }
}
