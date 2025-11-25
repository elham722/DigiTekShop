using DigiTekShop.Contracts.Abstractions.Identity.Admin;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Identity.Context;
using DigiTekShop.Identity.Models;
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

    public async Task<Result<PagedResponse<AdminUserListItemDto>>> GetUsersAsync(AdminUserListQuery query, CancellationToken ct)
    {
        var sanitizedPage = Math.Max(1, query.Page);
        var sanitizedSize = Math.Clamp(query.PageSize, 1, 100);
        var search = query.Search?.Trim();
        var statusFilter = query.Status?.Trim();

        var users = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search}%";
            users = users.Where(u =>
                (u.UserName != null && EF.Functions.Like(u.UserName, like)) ||
                (u.PhoneNumber != null && EF.Functions.Like(u.PhoneNumber, like)) ||
                (u.Email != null && EF.Functions.Like(u.Email, like)));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            var normalizedStatus = statusFilter.ToLowerInvariant();
            if (normalizedStatus is "active")
            {
                users = users.Where(u =>
                    (!u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow) &&
                    !u.IsDeleted);
            }
            else if (normalizedStatus is "locked")
            {
                users = users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            }
        }

        var totalCount = await users.CountAsync(ct);

        var pagedUsers = await users
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((sanitizedPage - 1) * sanitizedSize)
            .Take(sanitizedSize)
            .ToListAsync(ct);

        var userIds = pagedUsers.Select(u => u.Id).ToList();

        var rolesLookup = await _dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(ct);

        var groupedRoles = rolesLookup
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? string.Empty).ToArray());

        var items = new List<AdminUserListItemDto>(pagedUsers.Count);
        foreach (var user in pagedUsers)
        {
            items.Add(new AdminUserListItemDto
            {
                Id = user.Id,
                FullName = user.UserName,
                Phone = user.PhoneNumber ?? string.Empty,
                Email = user.Email,
                IsPhoneConfirmed = user.PhoneNumberConfirmed,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                CreatedAtUtc = user.CreatedAtUtc.UtcDateTime,
                LastLoginAtUtc = user.LastLoginAtUtc?.UtcDateTime,
                Roles = groupedRoles.TryGetValue(user.Id, out var roles)
                    ? roles
                    : Array.Empty<string>()
            });
        }

        _logger.LogDebug("Admin users list fetched. count={Count} page={Page} size={Size}", items.Count, sanitizedPage, sanitizedSize);

        var response = PagedResponse<AdminUserListItemDto>.Create(items, totalCount, new PagedRequest(Page: sanitizedPage, Size: sanitizedSize));

        return Result<PagedResponse<AdminUserListItemDto>>.Success(response);
    }
}

