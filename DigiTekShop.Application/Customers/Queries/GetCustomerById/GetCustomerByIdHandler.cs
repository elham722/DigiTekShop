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

        // Map to DTO via Mapster (in-memory mapping)
        var view = customer.Adapt<CustomerView>();

        return Result<CustomerView?>.Success(view);
    }
}