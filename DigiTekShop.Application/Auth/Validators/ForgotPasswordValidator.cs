using DigiTekShop.Contracts.DTOs.Auth.ResetPassword;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators
{
    public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequestDto>
    {
        public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
