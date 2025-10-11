using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressHandler : IRequestHandler<SetDefaultAddressCommand, Result>
{
    private readonly ICustomerRepository _repo;
    private readonly IUnitOfWork _uow;

    public SetDefaultAddressHandler(ICustomerRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        var id = new CustomerId(request.CustomerId);
        var customer = await _repo.GetByIdAsync(id, ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        var r = customer.SetDefaultAddress(request.Index);
        if (r.IsFailure) return r;

        await _repo.UpdateAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}