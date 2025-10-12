using DigiTekShop.Contracts.DTOs.Customer;
using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;

public sealed class GetMyCustomerProfileHandler : IQueryHandler<GetMyCustomerProfileQuery, CustomerView?>
{
    private readonly ICustomerQueryRepository _queryRepo;

    public GetMyCustomerProfileHandler(ICustomerQueryRepository queryRepo)
    {
        _queryRepo = queryRepo;
    }

    public async Task<Result<CustomerView?>> Handle(GetMyCustomerProfileQuery request, CancellationToken ct)
    {
        // Get customer by UserId (AsNoTracking)
        var customer = await _queryRepo.GetByUserIdAsync(request.UserId, ct);

        if (customer is null)
            return Result<CustomerView?>.Success(null);

        // Map to DTO via Mapster (in-memory mapping)
        var view = customer.Adapt<CustomerView>();

        return Result<CustomerView?>.Success(view);
    }
}