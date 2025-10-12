using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;

public sealed class GetMyCustomerProfileHandler
    : IRequestHandler<GetMyCustomerProfileQuery, CustomerView?>
{
    public Task<CustomerView?> Handle(GetMyCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}