using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.SharedKernel.Results;
using MediatR;

namespace DigiTekShop.Application.Customers.Commands.ChangeEmail;

public sealed class ChangeEmailHandler : IRequestHandler<ChangeEmailCommand, Result>
{
    private readonly ICustomerRepository _repo;
    private readonly IUnitOfWork _uow;

    public ChangeEmailHandler(ICustomerRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(ChangeEmailCommand request, CancellationToken ct)
    {
        var id = new CustomerId(request.CustomerId);
        var customer = await _repo.GetByIdAsync(id, ct);
        if (customer is null)
            return Result.Failure("Customer not found.");

        var existingByEmail = await _repo.GetByEmailAsync(request.NewEmail, ct);
        if (existingByEmail is not null && existingByEmail.Id.Value != request.CustomerId)
            return Result.Failure("Email already in use by another customer.");

        var r = customer.ChangeEmail(request.NewEmail);
        if (r.IsFailure) return r;

        await _repo.UpdateAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}