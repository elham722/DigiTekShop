using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.UpdateProfile;

public sealed class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly ICustomerRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateProfileHandler(ICustomerRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var id = new CustomerId(request.CustomerId);
        var customer = await _repo.GetByIdAsync(id, ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        var r = customer.UpdateProfile(request.FullName, request.Phone);
        if (r.IsFailure) return r;

        await _repo.UpdateAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}