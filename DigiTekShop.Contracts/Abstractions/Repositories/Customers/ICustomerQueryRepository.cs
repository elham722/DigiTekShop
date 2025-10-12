using DigiTekShop.Contracts.Abstractions.Repositories.Common.Query;

namespace DigiTekShop.Contracts.Abstractions.Repositories.Customers;

public interface ICustomerQueryRepository : IQueryRepository<Customer, CustomerId>
{
    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}