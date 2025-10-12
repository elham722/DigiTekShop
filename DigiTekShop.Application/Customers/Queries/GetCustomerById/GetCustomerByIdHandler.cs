using DigiTekShop.Contracts.DTOs.Customer;
using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdHandler : IQueryHandler<GetCustomerByIdQuery, CustomerView?>
{
    private readonly ICustomerQueryRepository _queryRepo;

    public GetCustomerByIdHandler(ICustomerQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<Result<CustomerView?>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        // Get customer with addresses (AsNoTracking)
        var customer = await _queryRepo.GetByIdAsync(
            customerId,
            includes: new Expression<Func<Customer, object>>[] { c => c.Addresses },
            ct: ct);

        if (customer is null)
            return Result<CustomerView?>.Success(null);

        // Map to DTO
        var view = new CustomerView(
            Id: customer.Id.Value,
            UserId: customer.UserId,
            FullName: customer.FullName,
            Email: customer.Email,
            Phone: customer.Phone,
            Addresses: customer.Addresses.Select(a => new AddressDto(
                Line1: a.Line1,
                Line2: a.Line2,
                City: a.City,
                State: a.State,
                PostalCode: a.PostalCode,
                Country: a.Country,
                IsDefault: a.IsDefault
            )).ToList());

        return Result<CustomerView?>.Success(view);
    }
}