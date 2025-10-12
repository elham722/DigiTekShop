using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Customers.Commands.AddAddress;

public sealed class AddAddressHandler : ICommandHandler<AddAddressCommand>
{
    private readonly ICustomerQueryRepository _queryRepo;
    private readonly ICustomerCommandRepository _commandRepo;

    public AddAddressHandler(
        ICustomerQueryRepository queryRepo,
        ICustomerCommandRepository commandRepo)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
    }

    public async Task<Result> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        // Get customer (AsNoTracking)
        var customer = await _queryRepo.GetByIdAsync(customerId, ct: ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        // Map DTO to Value Object
        var addressDto = request.Address;
        var address = new Address(
            line1: addressDto.Line1,
            line2: addressDto.Line2,
            city: addressDto.City,
            state: addressDto.State,
            postalCode: addressDto.PostalCode,
            country: addressDto.Country,
            isDefault: addressDto.IsDefault);

        // Add address using domain logic
        var addResult = customer.AddAddress(address, request.AsDefault);
        if (addResult.IsFailure)
            return addResult;

        // Update customer (SaveChanges called by UnitOfWork)
        await _commandRepo.UpdateAsync(customer, ct);

        return Result.Success();
    }
}