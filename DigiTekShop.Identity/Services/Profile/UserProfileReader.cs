using DigiTekShop.Contracts.Abstractions.Profile;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Identity.Services.Profile;

/// <summary>
/// خواندن اطلاعات پروفایل از Identity DB
/// </summary>
public sealed class UserProfileReader : IUserProfileReader
{
    private readonly DigiTekShopIdentityDbContext _dbContext;
    private readonly ILogger<UserProfileReader> _logger;

    public UserProfileReader(
        DigiTekShopIdentityDbContext dbContext,
        ILogger<UserProfileReader> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UserProfileData?> GetUserDataAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileData
            {
                UserId = u.Id,
                CustomerId = u.CustomerId,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                CreatedAtUtc = u.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        return user;
    }

    public async Task<bool> SetCustomerIdAsync(Guid userId, Guid customerId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found when setting CustomerId", userId);
            return false;
        }

        user.SetCustomerId(customerId);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CustomerId {CustomerId} set for User {UserId}",
            customerId, userId);

        return true;
    }
}

