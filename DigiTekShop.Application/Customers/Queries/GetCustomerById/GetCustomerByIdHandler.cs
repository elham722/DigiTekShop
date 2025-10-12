using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler
    : IRequestHandler<GetCustomerByIdQuery, CustomerView?>
{
    public Task<CustomerView?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}