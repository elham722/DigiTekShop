using DigiTekShop.Contracts.DTOs.Customer;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.AddAddress;

public sealed record AddAddressCommand(
    Guid CustomerId,
    AddressDto Address,
    bool AsDefault = false
) : IRequest<Result>;