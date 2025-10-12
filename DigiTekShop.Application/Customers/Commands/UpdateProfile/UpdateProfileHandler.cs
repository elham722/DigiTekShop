using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Commands.UpdateProfile;

public sealed class UpdateProfileHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly ICustomerQueryRepository _queryRepo;
    private readonly ICustomerCommandRepository _commandRepo;

    public UpdateProfileHandler(
        ICustomerQueryRepository queryRepo,
        ICustomerCommandRepository commandRepo)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        // Get customer (AsNoTracking)
        var customer = await _queryRepo.GetByIdAsync(customerId, ct: ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        // Update profile using domain logic
        var updateResult = customer.UpdateProfile(request.FullName, request.Phone);
        if (updateResult.IsFailure)
            return updateResult;

        // Update customer (SaveChanges called by UnitOfWork)
        await _commandRepo.UpdateAsync(customer, ct);

        return Result.Success();
    }
}