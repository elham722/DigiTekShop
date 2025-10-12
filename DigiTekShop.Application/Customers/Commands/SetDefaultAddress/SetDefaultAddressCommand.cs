namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(Guid CustomerId, int AddressIndex)
    : ICommand;