using FluentValidation;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Input.UserId).NotEmpty();
        RuleFor(x => x.Input.FullName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Input.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Input.Phone).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Input.Phone));
    }
}