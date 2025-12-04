using DigiTekShop.Contracts.Abstractions.Identity.Profile;

namespace DigiTekShop.Identity.Services.Profile;

public sealed class UserProfileService : IUserProfileService
{
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly ILogger<UserProfileService> _log;

    private static class Events
    {
        public static readonly EventId LinkCustomer = new(30010, "LinkCustomerToUser");
        public static readonly EventId HasProfile = new(30011, "HasProfile");
    }

    public UserProfileService(
        DigiTekShopIdentityDbContext db,
        ILogger<UserProfileService> log)
    {
        _db = db;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<Result> LinkCustomerToUserAsync(Guid userId, Guid customerId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            _log.LogWarning(Events.LinkCustomer, "User not found to link CustomerId. userId={UserId}", userId);
            return Result.Failure("کاربر یافت نشد", ErrorCodes.Identity.USER_NOT_FOUND);
        }

        if (user.CustomerId.HasValue)
        {
            _log.LogWarning(Events.LinkCustomer, "User already has CustomerId. userId={UserId}, existingCustomerId={ExistingCustomerId}",
                userId, user.CustomerId);
            return Result.Failure("پروفایل قبلاً تکمیل شده است", ErrorCodes.Profile.PROFILE_ALREADY_COMPLETE);
        }

        user.SetCustomerId(customerId);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(Events.LinkCustomer, "Linked CustomerId to User. userId={UserId}, customerId={CustomerId}",
            userId, customerId);

        return Result.Success();
    }

    public async Task<bool> HasProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var hasProfile = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CustomerId.HasValue)
            .FirstOrDefaultAsync(ct);

        _log.LogDebug(Events.HasProfile, "HasProfile check. userId={UserId}, hasProfile={HasProfile}", userId, hasProfile);

        return hasProfile;
    }
}

