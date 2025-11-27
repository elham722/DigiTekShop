using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Identity.Services.Search;

public sealed class UserDataProvider : IUserDataProvider
{
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserDataProvider> _logger;

    public UserDataProvider(
        DigiTekShopIdentityDbContext db,
        UserManager<User> userManager,
        ILogger<UserDataProvider> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        return await _db.Users.CountAsync(ct);
    }

    public async Task<IReadOnlyList<UserSearchDocument>> GetUsersBatchAsync(
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var users = await _db.Users
            .OrderBy(u => u.Id)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(ct);

        var docs = new List<UserSearchDocument>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            docs.Add(MapToDocument(user, roles.ToArray()));
        }

        return docs;
    }

    public async Task<UserSearchDocument?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDocument(user, roles.ToArray());
    }

    private static UserSearchDocument MapToDocument(User user, string[] roles)
    {
        return new UserSearchDocument
        {
            Id = user.Id.ToString(),
            FullName = user.UserName,
            Phone = user.PhoneNumber ?? string.Empty,
            Email = user.Email,
            IsPhoneConfirmed = user.PhoneNumberConfirmed,
            IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            IsDeleted = user.IsDeleted,
            CreatedAtUtc = user.CreatedAtUtc.UtcDateTime,
            LastLoginAtUtc = user.LastLoginAtUtc?.UtcDateTime,
            Roles = roles
        };
    }
}

