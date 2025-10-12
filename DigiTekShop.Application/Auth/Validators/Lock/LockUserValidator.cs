using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Application.Auth.Validators.Lock
{
    public sealed class LockUserValidator : AbstractValidator<LockUserRequestDto>
    {
        public LockUserValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.LockoutEnd).Must(e => !e.HasValue || e.Value > DateTimeOffset.UtcNow)
                .WithMessage("LockoutEnd must be in the future");
        }
    }
}
