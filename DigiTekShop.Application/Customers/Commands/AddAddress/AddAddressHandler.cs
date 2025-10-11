using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.Domain.Customers.ValueObjects;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.AddAddress;

public sealed class AddAddressHandler : IRequestHandler<AddAddressCommand, Result>
{
    private readonly ICustomerRepository _repo;
    private readonly IUnitOfWork _uow;

    public AddAddressHandler(ICustomerRepository repo, IUnitOfWork uow)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<Result> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var id = new CustomerId(request.CustomerId);
        var customer = await _repo.GetByIdAsync(id, ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        var a = request.Address;
        var vo = new Address(a.Line1, a.Line2, a.City, a.State, a.PostalCode, a.Country, a.IsDefault);
        var r = customer.AddAddress(vo, request.AsDefault);
        if (r.IsFailure) return r;

        await _repo.UpdateAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}