
using DigiTekShop.Contracts.DTOs.Auth.Register;
using FluentValidation;

namespace DigiTekShop.Application.Auth.Validators;

public class RegisterValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
        RuleFor(x => x.AcceptTerms).Equal(true);
        RuleFor(x => x.DeviceId).MaximumLength(100).When(x => x.DeviceId is not null);
    }
}