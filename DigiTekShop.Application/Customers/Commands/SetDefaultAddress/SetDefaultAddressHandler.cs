using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressHandler : ICommandHandler<SetDefaultAddressCommand>
{
    private readonly ICustomerQueryRepository _queryRepo;
    private readonly ICustomerCommandRepository _commandRepo;

    public SetDefaultAddressHandler(
        ICustomerQueryRepository queryRepo,
        ICustomerCommandRepository commandRepo)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        // Get customer (AsNoTracking)
        var customer = await _queryRepo.GetByIdAsync(customerId, ct: ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        // Set default address using domain logic
        var setDefaultResult = customer.SetDefaultAddress(request.AddressIndex);
        if (setDefaultResult.IsFailure)
            return setDefaultResult;

        // Update customer (SaveChanges called by UnitOfWork)
        await _commandRepo.UpdateAsync(customer, ct);

        return Result.Success();
    }
}