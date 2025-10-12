using DigiTekShop.Contracts.DTOs.Customer;

namespace DigiTekShop.Application.Customers.Queries.GetCustomerById;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerView?>;