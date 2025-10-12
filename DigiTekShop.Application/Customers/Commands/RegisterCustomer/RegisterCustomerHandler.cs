using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed class RegisterCustomerHandler
    : IRequestHandler<RegisterCustomerCommand, Result<Guid>>
{
    private readonly ICustomerQueryRepository _q;
    private readonly ICustomerCommandRepository _c;

    public RegisterCustomerHandler(ICustomerQueryRepository q, ICustomerCommandRepository c)
    { _q = q; _c = c; }

    public async Task<Result<Guid>> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var input = request.Input;

        if (await _q.GetByUserIdAsync(input.UserId, ct) is not null)
            return Result<Guid>.Failure("Customer already exists for this user.");

        if (await _q.GetByEmailAsync(input.Email, ct) is not null)
            return Result<Guid>.Failure("Email already registered as customer.");

        var customer = Customer.Register(input.UserId, input.FullName, input.Email, input.Phone);
        await _c.AddAsync(customer, ct);
        return Result<Guid>.Success(customer.Id.Value);
    }
}