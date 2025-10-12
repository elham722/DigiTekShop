using DigiTekShop.Contracts.Customer;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed record RegisterCustomerCommand(RegisterCustomerDto Input)
    : IRequest<Result<Guid>>;