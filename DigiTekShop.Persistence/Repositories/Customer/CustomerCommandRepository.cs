using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Domain.Customer.ValueObjects;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Persistence.Repositories.Customer;

public sealed class CustomerCommandRepository
    : EfCommandRepository<Domain.Customer.Entities.Customer, CustomerId>, 
      ICustomerCommandRepository
{
    private readonly DigiTekShopDbContext _ctx;
    private readonly ILogger<CustomerCommandRepository> _logger;

    public CustomerCommandRepository(
        DigiTekShopDbContext ctx,
        ILogger<CustomerCommandRepository> logger) : base(ctx)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<Result> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string? email,
        string? phone,
        CancellationToken ct = default)
    {
        var customer = await _ctx.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (customer is null)
        {
            _logger.LogWarning("Customer not found for user {UserId}", userId);
            return Result.Failure(
                "پروفایل کاربر یافت نشد",
                ErrorCodes.Profile.PROFILE_NOT_FOUND);
        }

        // آپدیت نام و تلفن
        var updateResult = customer.UpdateProfile(fullName, phone);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning("UpdateProfile failed: {Error}", updateResult.GetErrorsAsString());
            return updateResult;
        }

        // آپدیت ایمیل
        var emailResult = customer.ChangeEmail(email);
        if (emailResult.IsFailure)
        {
            _logger.LogWarning("ChangeEmail failed: {Error}", emailResult.GetErrorsAsString());
            return emailResult;
        }

        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return Result.Success();
    }
}