using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Commands.RegisterCustomer;

public sealed class RegisterCustomerHandler : ICommandHandler<RegisterCustomerCommand, Guid>
{
    private readonly ICustomerQueryRepository _queryRepo;
    private readonly ICustomerCommandRepository _commandRepo;

    public RegisterCustomerHandler(
        ICustomerQueryRepository queryRepo,
        ICustomerCommandRepository commandRepo)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public async Task<Result<Guid>> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var input = request.Input;

        // Check if customer already exists for this user
        var existingByUser = await _queryRepo.GetByUserIdAsync(input.UserId, ct);
        if (existingByUser is not null)
            return Result<Guid>.Failure("Customer already exists for this user.");

        // Check if email is already registered
        var existingByEmail = await _queryRepo.GetByEmailAsync(input.Email, ct);
        if (existingByEmail is not null)
            return Result<Guid>.Failure("Email already registered as customer.");

        // Create new customer using factory method
        var customer = Customer.Register(
            userId: input.UserId,
            fullName: input.FullName,
            email: input.Email,
            phone: input.Phone);

        // Add to repository (SaveChanges called by UnitOfWork)
        await _commandRepo.AddAsync(customer, ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}