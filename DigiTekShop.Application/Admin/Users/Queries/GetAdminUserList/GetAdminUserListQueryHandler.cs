using DigiTekShop.Contracts.Abstractions.Identity.Admin;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;

public sealed class GetAdminUserListQueryHandler
    : IQueryHandler<GetAdminUserListQuery, PagedResponse<AdminUserListItemDto>>
{
    private readonly IAdminUserReadService _adminUserReadService;

    public GetAdminUserListQueryHandler(IAdminUserReadService adminUserReadService)
        => _adminUserReadService = adminUserReadService;

    public Task<Result<PagedResponse<AdminUserListItemDto>>> Handle(GetAdminUserListQuery request, CancellationToken ct)
        => _adminUserReadService.GetUsersAsync(request.Filters, ct);
}

