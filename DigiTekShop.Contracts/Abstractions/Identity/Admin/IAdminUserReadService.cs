using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.Contracts.Abstractions.Identity.Admin;

public interface IAdminUserReadService
{
    Task<Result<PagedResponse<AdminUserListItemDto>>> GetUsersAsync(AdminUserListQuery query, CancellationToken ct);
    Task<Result<AdminUserDetailsDto>> GetUserDetailsAsync(Guid userId, CancellationToken ct);
}

