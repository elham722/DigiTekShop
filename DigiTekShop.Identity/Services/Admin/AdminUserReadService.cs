using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Identity.Extensions;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Identity.Services.Admin;

public sealed class AdminUserReadService : IAdminUserReadService
{
    private readonly DigiTekShopIdentityDbContext _dbContext;
    private readonly ILogger<AdminUserReadService> _logger;

    public AdminUserReadService(
        DigiTekShopIdentityDbContext dbContext,
        ILogger<AdminUserReadService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PagedResponse<AdminUserListItemDto>>> GetUsersAsync(
       AdminUserListQuery query, CancellationToken ct)
    {
        var pagedRequest = new PagedRequest(
            Page: query.Page,
            Size: query.PageSize,
            SortBy: null,
            Ascending: true,
            SearchTerm: query.Search
        ).Normalize();

        var usersQuery = _dbContext.Users.AsNoTracking();

        //  search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var rawSearch = query.Search.Trim();


            if (Normalization.TryNormalizePhoneIranE164(rawSearch, out var e164) && e164 is not null)
            {
                var last10 = e164[^10..];

                usersQuery = usersQuery.Where(u =>
                    u.PhoneNumber != null &&
                    (
                        u.PhoneNumber == e164 ||
                        EF.Functions.Like(u.PhoneNumber, e164 + "%") ||
                        EF.Functions.Like(u.PhoneNumber, "%" + last10 + "%")
                    ));
            }
            else
            {

                var digits = Normalization.StripNonDigits(
                    Normalization.ToLatinDigits(rawSearch)
                );

                if (!string.IsNullOrEmpty(digits) && digits.Length >= 3)
                {

                    var digitsNoZero = digits;
                    if (digitsNoZero.StartsWith("09"))
                        digitsNoZero = digitsNoZero[1..];

                    usersQuery = usersQuery.Where(u =>
                        u.PhoneNumber != null &&
                        (
                            EF.Functions.Like(u.PhoneNumber, "%" + digits + "%") ||
                            EF.Functions.Like(u.PhoneNumber, "%" + digitsNoZero + "%")
                        ));
                }
                else
                {
                    var like = $"%{rawSearch}%";
                    usersQuery = usersQuery.Where(u =>
                        (u.UserName != null && EF.Functions.Like(u.UserName, like)) ||
                        (u.Email != null && EF.Functions.Like(u.Email, like)));
                }
            }
        }


        //  status filter
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var normalizedStatus = query.Status.Trim().ToLowerInvariant();
            if (normalizedStatus is "active")
            {
                usersQuery = usersQuery.Where(u =>
                    (!u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow) &&
                    !u.IsDeleted);
            }
            else if (normalizedStatus is "locked")
            {
                usersQuery = usersQuery.Where(u =>
                    u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            }
        }

        //  date filters
        if (query.CreatedAtFrom.HasValue)
        {
            var fromDate = new DateTimeOffset(query.CreatedAtFrom.Value.Date, TimeSpan.Zero);
            usersQuery = usersQuery.Where(u => u.CreatedAtUtc >= fromDate);
        }

        if (query.CreatedAtTo.HasValue)
        {
            var toDate = new DateTimeOffset(query.CreatedAtTo.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);
            usersQuery = usersQuery.Where(u => u.CreatedAtUtc <= toDate);
        }

        if (query.LastLoginAtFrom.HasValue)
        {
            var fromDate = new DateTimeOffset(query.LastLoginAtFrom.Value.Date, TimeSpan.Zero);
            usersQuery = usersQuery.Where(u => u.LastLoginAtUtc.HasValue && u.LastLoginAtUtc >= fromDate);
        }

        if (query.LastLoginAtTo.HasValue)
        {
            var toDate = new DateTimeOffset(query.LastLoginAtTo.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);
            usersQuery = usersQuery.Where(u => u.LastLoginAtUtc.HasValue && u.LastLoginAtUtc <= toDate);
        }

        // Get users with pagination first
        var usersWithRoles = await usersQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new
            {
                User = u,
                UserId = u.Id
            })
            .ToPagedResponseAsync(pagedRequest, ct);

        // Get all user IDs from the current page
        var userIds = usersWithRoles.Items.Select(x => x.UserId).ToList();

        // Load roles for all users in one query (avoiding N+1)
        var userRolesDict = await _dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(
                _dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name }
            )
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RoleName).ToArray(),
                ct);

        // Map to DTOs with roles
        var items = usersWithRoles.Items.Select(x => new AdminUserListItemDto
        {
            Id = x.User.Id,
            FullName = x.User.UserName,
            Phone = x.User.PhoneNumber ?? string.Empty,
            Email = x.User.Email,
            IsPhoneConfirmed = x.User.PhoneNumberConfirmed,
            IsLocked = x.User.LockoutEnd.HasValue && x.User.LockoutEnd > DateTimeOffset.UtcNow,
            CreatedAtUtc = x.User.CreatedAtUtc.UtcDateTime,
            LastLoginAtUtc = x.User.LastLoginAtUtc.HasValue
                ? x.User.LastLoginAtUtc.Value.UtcDateTime : (DateTime?)null,
            Roles = userRolesDict.GetValueOrDefault(x.UserId, Array.Empty<string>())
        }).ToList();

        var paged = PagedResponse<AdminUserListItemDto>.Create(
            items,
            usersWithRoles.TotalCount,
            pagedRequest);

        return Result<PagedResponse<AdminUserListItemDto>>.Success(paged);
    }

    public async Task<Result<AdminUserDetailsDto>> GetUserDetailsAsync(Guid userId, CancellationToken ct)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<AdminUserDetailsDto>.Failure(ErrorCodes.Identity.USER_NOT_FOUND);

        // گرفتن نقش‌ها
        var userRoles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                _dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name
            )
            .ToArrayAsync(ct);

        var lockoutEnd = user.LockoutEnd;
        var isLocked = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow;

        var dto = new AdminUserDetailsDto
        {
            Id = user.Id,
            FullName = user.UserName,
            Phone = user.PhoneNumber ?? string.Empty,
            Email = user.Email,
            IsPhoneConfirmed = user.PhoneNumberConfirmed,
            IsLocked = isLocked,
            IsDeleted = user.IsDeleted,
            CreatedAtUtc = user.CreatedAtUtc.UtcDateTime,
            LastLoginAtUtc = user.LastLoginAtUtc?.UtcDateTime,
            Roles = userRoles,
            LockoutEnd = lockoutEnd,
            AccessFailedCount = user.AccessFailedCount
        };

        return Result<AdminUserDetailsDto>.Success(dto);
    }

}

