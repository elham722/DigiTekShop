using DigiTekShop.Application.Customers.Commands.ChangeEmail;
using FluentValidation;

namespace DigiTekShop.Application.Customers.Validators;

public sealed class ChangeEmailValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}