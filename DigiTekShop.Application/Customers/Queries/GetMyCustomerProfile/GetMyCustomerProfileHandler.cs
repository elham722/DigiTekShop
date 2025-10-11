using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;

public sealed class GetMyCustomerProfileHandler
    : IRequestHandler<GetMyCustomerProfileQuery, CustomerView?>
{
    private readonly ICustomerRepository _repo;

    public GetMyCustomerProfileHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerView?> Handle(GetMyCustomerProfileQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByUserIdAsync(request.UserId, ct);
        if (c is null) return null;

        var addresses = c.Addresses
            .Select(a => new AddressDto(a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.IsDefault))
            .ToList();

        return new CustomerView(c.Id.Value, c.UserId, c.FullName, c.Email, c.Phone, addresses);
    }
}