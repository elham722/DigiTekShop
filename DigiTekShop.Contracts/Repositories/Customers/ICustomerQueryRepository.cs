// DigiTekShop.Contracts/Repositories/Customers/ICustomerQueryRepository.cs
using DigiTekShop.Contracts.Repositories.Query;
using DigiTekShop.Domain.Customer.Entities;

namespace DigiTekShop.Contracts.Repositories.Customers;

public interface ICustomerQueryRepository : IQueryRepository<Domain.Customer.Entities.Customer, CustomerId>
{
    Task<Domain.Customer.Entities.Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Domain.Customer.Entities.Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}