using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Identity.Extensions;

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

        //  projection
        var projectedQuery = usersQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                FullName = u.UserName,
                Phone = u.PhoneNumber ?? string.Empty,
                Email = u.Email,
                IsPhoneConfirmed = u.PhoneNumberConfirmed,
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAtUtc = u.CreatedAtUtc.UtcDateTime,
                LastLoginAtUtc = u.LastLoginAtUtc.HasValue
                ? u.LastLoginAtUtc.Value.UtcDateTime : (DateTime?)null,
                Roles = Array.Empty<string>()
            });

        var paged = await projectedQuery.ToPagedResponseAsync(pagedRequest, ct);

        return Result<PagedResponse<AdminUserListItemDto>>.Success(paged);
    }

}

