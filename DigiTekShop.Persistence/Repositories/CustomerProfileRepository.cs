using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CustomerEntity = DigiTekShop.Domain.Customer.Entities.Customer;
using CustomerId = DigiTekShop.Domain.Customer.Entities.CustomerId;

namespace DigiTekShop.Persistence.Repositories;

/// <summary>
/// عملیات Customer برای پروفایل
/// </summary>
public sealed class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly DigiTekShopDbContext _dbContext;
    private readonly ILogger<CustomerProfileRepository> _logger;

    public CustomerProfileRepository(
        DigiTekShopDbContext dbContext,
        ILogger<CustomerProfileRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _dbContext.Set<CustomerEntity>()
            .AsNoTracking()
            .AnyAsync(c => c.Id == new CustomerId(customerId), ct);
    }

    public async Task<CustomerProfileData?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _dbContext.Set<CustomerEntity>()
            .AsNoTracking()
            .Where(c => c.Id == new CustomerId(customerId))
            .Select(c => new CustomerProfileData
            {
                CustomerId = c.Id.Value,
                UserId = c.UserId,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone
            })
            .FirstOrDefaultAsync(ct);

        return customer;
    }

    public async Task<CustomerProfileData?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var customer = await _dbContext.Set<CustomerEntity>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new CustomerProfileData
            {
                CustomerId = c.Id.Value,
                UserId = c.UserId,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone
            })
            .FirstOrDefaultAsync(ct);

        return customer;
    }

    public async Task<Guid> CreateAsync(CreateCustomerData data, CancellationToken ct = default)
    {
        var customer = CustomerEntity.Register(
            userId: data.UserId,
            fullName: data.FullName,
            email: data.Email,
            phone: data.Phone);

        await _dbContext.Set<CustomerEntity>().AddAsync(customer, ct);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Customer {CustomerId} created for User {UserId}",
            customer.Id.Value, data.UserId);

        return customer.Id.Value;
    }

    public async Task<bool> UpdateAsync(Guid customerId, UpdateCustomerData data, CancellationToken ct = default)
    {
        var customer = await _dbContext.Set<CustomerEntity>()
            .FirstOrDefaultAsync(c => c.Id == new CustomerId(customerId), ct);

        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for update", customerId);
            return false;
        }

        customer.UpdateProfile(data.FullName, customer.Phone);

        if (!string.IsNullOrWhiteSpace(data.Email))
        {
            customer.ChangeEmail(data.Email);
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Customer {CustomerId} updated", customerId);
        return true;
    }
}

