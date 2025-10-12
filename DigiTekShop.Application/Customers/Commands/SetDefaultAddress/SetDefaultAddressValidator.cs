namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.AddressIndex).GreaterThanOrEqualTo(0);
    }
}