using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.ChangeEmail;

public sealed class ChangeEmailHandler : IRequestHandler<ChangeEmailCommand, Result>
{
    private readonly ICustomerQueryRepository _q;
    private readonly ICustomerCommandRepository _c;

    public ChangeEmailHandler(ICustomerQueryRepository q, ICustomerCommandRepository c)
    { _q = q; _c = c; }

    public async Task<Result> Handle(ChangeEmailCommand request, CancellationToken ct)
    {
        var customer = await _q.GetByIdAsync(new CustomerId(request.CustomerId), ct: ct);
        if (customer is null) return Result.Failure("Customer not found.");

        var other = await _q.GetByEmailAsync(request.NewEmail, ct);
        if (other is not null && other.Id.Value != request.CustomerId)
            return Result.Failure("Email already in use by another customer.");

        var r = customer.ChangeEmail(request.NewEmail);
        if (r.IsFailure) return r;

        await _c.UpdateAsync(customer, ct);
        return Result.Success();
    }
}