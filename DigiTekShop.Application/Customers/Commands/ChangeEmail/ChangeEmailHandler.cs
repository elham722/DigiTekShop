using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Commands.ChangeEmail;

public sealed class ChangeEmailHandler : ICommandHandler<ChangeEmailCommand>
{
    private readonly ICustomerQueryRepository _queryRepo;
    private readonly ICustomerCommandRepository _commandRepo;

    public ChangeEmailHandler(
        ICustomerQueryRepository queryRepo,
        ICustomerCommandRepository commandRepo)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public async Task<Result> Handle(ChangeEmailCommand request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        // Get customer (AsNoTracking)
        var customer = await _queryRepo.GetByIdAsync(customerId, ct: ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        // Check if new email is already in use by another customer
        var existingCustomer = await _queryRepo.GetByEmailAsync(request.NewEmail, ct);
        if (existingCustomer is not null && existingCustomer.Id.Value != request.CustomerId)
            return Result.Failure("Email already in use by another customer.");

        // Change email using domain logic
        var changeResult = customer.ChangeEmail(request.NewEmail);
        if (changeResult.IsFailure)
            return changeResult;

        // Update customer (SaveChanges called by UnitOfWork)
        await _commandRepo.UpdateAsync(customer, ct);

        return Result.Success();
    }
}