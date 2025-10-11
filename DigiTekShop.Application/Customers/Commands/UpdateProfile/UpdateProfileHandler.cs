using DigiTekShop.Contracts.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.UpdateProfile;

public sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly ICustomerQueryRepository _q;
    private readonly ICustomerCommandRepository _c;

    public UpdateProfileHandler(ICustomerQueryRepository q, ICustomerCommandRepository c)
    { _q = q; _c = c; }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var customer = await _q.GetByIdAsync(new CustomerId(request.CustomerId), ct: ct);
        if (customer is null) return Result.Failure("Customer not found.");

        var r = customer.UpdateProfile(request.FullName, request.Phone);
        if (r.IsFailure) return r;

        await _c.UpdateAsync(customer, ct);
        return Result.Success();
    }
}