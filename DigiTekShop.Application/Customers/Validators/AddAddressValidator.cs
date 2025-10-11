using DigiTekShop.Application.Customers.Commands.AddAddress;
using FluentValidation;

namespace DigiTekShop.Application.Customers.Validators;

public sealed class AddAddressValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Address.Line1).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Address.PostalCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(64);
    }
}