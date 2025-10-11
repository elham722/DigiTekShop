// DigiTekShop.Contracts/Repositories/Customers/ICustomerQueryRepository.cs
using DigiTekShop.Contracts.Repositories.Query;
using DigiTekShop.Domain.Customer.Entities;

namespace DigiTekShop.Contracts.Repositories.Customers;

public interface ICustomerQueryRepository : IQueryRepository<Customer, CustomerId>
{
    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}