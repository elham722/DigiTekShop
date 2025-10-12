using DigiTekShop.Contracts.Customer;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerView?>;