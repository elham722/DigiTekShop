using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed class RegisterCustomerHandler
    : IRequestHandler<RegisterCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _repo;
    private readonly IUnitOfWork _uow;

    public RegisterCustomerHandler(ICustomerRepository repo, IUnitOfWork uow)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<Result<Guid>> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var input = request.Input;

        var existing = await _repo.GetByUserIdAsync(input.UserId, ct);
        if (existing is not null)
            return Result<Guid>.Failure("Customer already exists for this user.");

        var existingByEmail = await _repo.GetByEmailAsync(input.Email, ct);
        if (existingByEmail is not null)
            return Result<Guid>.Failure("Email already registered as customer.");

        var customer = Customer.Register(input.UserId, input.FullName, input.Email, input.Phone);
        await _repo.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}