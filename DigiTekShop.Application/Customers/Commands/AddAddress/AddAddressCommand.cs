using DigiTekShop.Contracts.DTOs.Customer;

namespace DigiTekShop.Application.Customers.Commands.AddAddress;

public sealed record AddAddressCommand(
    Guid CustomerId,
    AddressDto Address,
    bool AsDefault = false
) : ICommand;