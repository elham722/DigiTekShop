using DigiTekShop.Contracts.DTOs.Customer;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed record RegisterCustomerCommand(RegisterCustomerDto Input)
    : ICommand<Guid>;