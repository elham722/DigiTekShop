using DigiTekShop.Contracts.DTOs.Customer;
using DigiTekShop.Domain.Customers.Entities;
using MediatR;

namespace DigiTekShop.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler
    : IRequestHandler<GetCustomerByIdQuery, CustomerView?>
{
    private readonly ICustomerRepository _repo;

    public GetCustomerByIdHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<CustomerView?> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var id = new CustomerId(request.CustomerId);
        var c = await _repo.GetByIdAsync(id, ct);
        if (c is null) return null;

        var addresses = c.Addresses
            .Select(a => new AddressDto(a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.IsDefault))
            .ToList();

        return new CustomerView(c.Id.Value, c.UserId, c.FullName, c.Email, c.Phone, addresses);
    }
}