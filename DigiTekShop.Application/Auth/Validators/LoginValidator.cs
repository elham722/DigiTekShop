using DigiTekShop.Contracts.DTOs.Auth.Login;
using FluentValidation;

namespace DigiTekShop.Application.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginRequestDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.DeviceId).MaximumLength(100).When(x => x.DeviceId is not null);
        RuleFor(x => x.UserAgent).MaximumLength(400).When(x => x.UserAgent is not null);
        RuleFor(x => x.Ip).MaximumLength(100).When(x => x.Ip is not null);
    }
}