using DigiTekShop.Contracts.DTOs.Auth.Logout;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators
{
    public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequestDto>
    {
        public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
