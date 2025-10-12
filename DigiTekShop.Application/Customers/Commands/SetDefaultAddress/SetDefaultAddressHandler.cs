using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressHandler : IRequestHandler<SetDefaultAddressCommand, Result>
{
    private readonly ICustomerQueryRepository _q;
    private readonly ICustomerCommandRepository _c;

    public SetDefaultAddressHandler(ICustomerQueryRepository q, ICustomerCommandRepository c)
    { _q = q; _c = c; }

    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        var customer = await _q.GetByIdAsync(new CustomerId(request.CustomerId), ct: ct);
        if (customer is null) return Result.Failure("Customer not found.");

        var r = customer.SetDefaultAddress(request.Index);
        if (r.IsFailure) return r;

        await _c.UpdateAsync(customer, ct);
        return Result.Success();
    }
}