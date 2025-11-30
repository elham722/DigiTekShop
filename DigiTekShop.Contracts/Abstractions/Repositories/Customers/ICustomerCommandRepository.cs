using DigiTekShop.Contracts.Abstractions.Repositories.Common.Command;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Abstractions.Repositories.Customers;

public interface ICustomerCommandRepository : ICommandRepository<Customer, CustomerId>
{
    /// <summary>
    /// آپدیت پروفایل کاربر (نام، ایمیل، تلفن)
    /// </summary>
    Task<Result> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string? email,
        string? phone,
        CancellationToken ct = default);
}