using DigiTekShop.Contracts.Abstractions.Repositories.Common.Query;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Contracts.Abstractions.Repositories.Customers;

public interface ICustomerQueryRepository : IQueryRepository<Customer, CustomerId>
{
    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// دریافت پروفایل کاربر با Projection (شامل آدرس‌ها)
    /// </summary>
    Task<MyProfileDto?> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default);
}