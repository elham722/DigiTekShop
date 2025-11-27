using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;

public sealed class GetAdminUserListQueryHandler
    : IQueryHandler<GetAdminUserListQuery, PagedResponse<AdminUserListItemDto>>
{
    private readonly IUserSearchService _userSearchService;

    public GetAdminUserListQueryHandler(IUserSearchService userSearchService)
    {
        _userSearchService = userSearchService;
    }

    public async Task<Result<PagedResponse<AdminUserListItemDto>>> Handle(
        GetAdminUserListQuery request,
        CancellationToken ct)
    {
        var q = request.Filters;
        var page = q.Page <= 0 ? 1 : q.Page;
        var pageSize = q.PageSize <= 0 ? 20 : q.PageSize;
        var searchTerm = q.Search ?? string.Empty;

        // جستجو در Elasticsearch
        var searchResult = await _userSearchService.SearchAsync(searchTerm, page, pageSize, ct);

        if (!searchResult.IsSuccess)
        {
            return Result<PagedResponse<AdminUserListItemDto>>.Failure(
                searchResult.Errors,
                searchResult.ErrorCode);
        }

        var data = searchResult.Value;

        // Map از UserSearchDocument → AdminUserListItemDto
        var items = data.Items.Select(u => new AdminUserListItemDto
        {
            Id = Guid.Parse(u.Id),
            FullName = u.FullName,
            Phone = u.Phone,
            Email = u.Email,
            IsPhoneConfirmed = u.IsPhoneConfirmed,
            IsLocked = u.IsLocked,
            CreatedAtUtc = u.CreatedAtUtc,
            LastLoginAtUtc = u.LastLoginAtUtc,
            Roles = u.Roles ?? Array.Empty<string>()
        }).ToList();

        // ساخت PagedResponse
        var paged = PagedResponse<AdminUserListItemDto>.Create(
            items,
            data.TotalCount,
            new PagedRequest(page, pageSize, SortBy: null, Ascending: true, SearchTerm: searchTerm));

        return Result<PagedResponse<AdminUserListItemDto>>.Success(paged);
    }
}

