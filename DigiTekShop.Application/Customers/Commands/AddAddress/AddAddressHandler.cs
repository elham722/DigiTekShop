using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.SharedKernel.Errors;

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

        var customer = await _queryRepo.GetByIdAsync(customerId, ct: ct);
        if (customer is null)
            return Result.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        
        var addressDto = request.Address;
        var address = addressDto.Adapt<Address>();

        
        var addResult = customer.AddAddress(address, request.AsDefault);
        if (addResult.IsFailure)
            return addResult;

        await _commandRepo.UpdateAsync(customer, ct);

        return Result.Success();
    }
}