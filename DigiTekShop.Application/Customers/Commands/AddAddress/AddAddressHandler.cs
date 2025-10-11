using DigiTekShop.Contracts.Repositories.Customers;
using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.Domain.Customers.ValueObjects;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.AddAddress;

public sealed class AddAddressHandler : IRequestHandler<AddAddressCommand, Result>
{
    private readonly ICustomerQueryRepository _q;
    private readonly ICustomerCommandRepository _c;

    public AddAddressHandler(ICustomerQueryRepository q, ICustomerCommandRepository c)
    {
        _q = q; _c = c;
    }

    public async Task<Result> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var customer = await _q.GetByIdAsync(new CustomerId(request.CustomerId), ct: ct);
        if (customer is null) return Result.Failure("Customer not found.");

        var a = request.Address;
        var vo = new Address(a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.IsDefault);

        var op = customer.AddAddress(vo, request.AsDefault);
        if (op.IsFailure) return op;

        await _c.UpdateAsync(customer, ct); // ذخیره توسط Behavior
        return Result.Success();
    }
}