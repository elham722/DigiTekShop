using Microsoft.EntityFrameworkCore;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Domain.Customer.ValueObjects;
using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Persistence.Repositories.Customer;

public sealed class CustomerQueryRepository 
    : EfQueryRepository<Domain.Customer.Entities.Customer, CustomerId>, 
      ICustomerQueryRepository
{
    private readonly DigiTekShopDbContext _ctx;

    public CustomerQueryRepository(DigiTekShopDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public Task<Domain.Customer.Entities.Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _ctx.Set<Domain.Customer.Entities.Customer>()
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

   
    public Task<Domain.Customer.Entities.Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _ctx.Set<Domain.Customer.Entities.Customer>()
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<MyProfileDto?> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _ctx.Customers
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new MyProfileDto(
                c.UserId,
                c.Id.Value,
                c.FullName,
                c.Email,
                c.Phone,
                c.IsActive,
                c.Addresses
                    .Select(a => new MyAddressDto(
                        EF.Property<int>(a, "Id"),
                        a.Line1,
                        a.Line2,
                        a.City,
                        a.State,
                        a.PostalCode,
                        a.Country,
                        a.IsDefault
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        return profile;
    }
}