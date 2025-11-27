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
        // تمام نرمال‌سازی داخل ToCriteria()
        var criteria = request.Filters.ToCriteria();

        var searchResult = await _userSearchService.SearchAsync(criteria, ct);

        if (!searchResult.IsSuccess)
        {
            return Result<PagedResponse<AdminUserListItemDto>>.Failure(
                searchResult.Errors,
                searchResult.ErrorCode);
        }

        var data = searchResult.Value;

        // Map از UserSearchDocument → AdminUserListItemDto با Mapster
        var items = data.Items.Adapt<List<AdminUserListItemDto>>();

        // ساخت PagedResponse
        var paged = PagedResponse<AdminUserListItemDto>.Create(
            items,
            data.TotalCount,
            new PagedRequest(
                criteria.Page,
                criteria.PageSize,
                SortBy: null,
                Ascending: true,
                SearchTerm: criteria.Search));

        return Result<PagedResponse<AdminUserListItemDto>>.Success(paged);
    }
}

