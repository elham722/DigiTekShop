using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;

public sealed record GetAdminUserListQuery(AdminUserListQuery Filters)
    : IQuery<PagedResponse<AdminUserListItemDto>>;

