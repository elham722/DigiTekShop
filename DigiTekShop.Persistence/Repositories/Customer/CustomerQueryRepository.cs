// DigiTekShop.Infrastructure/Persistence/Ef/CustomerQueryRepository.cs
using Microsoft.EntityFrameworkCore;
using DigiTekShop.Contracts.Repositories.Customers;
using DigiTekShop.Domain.Customers.Entities;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;

namespace DigiTekShop.Persistence.Repositories.Customer;

public sealed class CustomerQueryRepository : EfQueryRepository<Domain.Customers.Entities.Customer, Domain.Customers.Entities.CustomerId>, ICustomerQueryRepository
{
    private readonly DigiTekShopDbContext _ctx;

    public CustomerQueryRepository(DigiTekShopDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public Task<Domain.Customers.Entities.Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _ctx.Set<Domain.Customers.Entities.Customer>().Include(x => x.Addresses).FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public Task<Domain.Customers.Entities.Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _ctx.Set<Domain.Customers.Entities.Customer>().Include(x => x.Addresses).FirstOrDefaultAsync(x => x.Email == email, ct);
}