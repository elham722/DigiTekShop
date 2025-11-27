using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Contracts.Abstractions.Search;

public interface IUserSearchService
{
    Task<Result<UserSearchResult>> SearchAsync(
        AdminUserSearchCriteria criteria,
        CancellationToken ct = default);

    Task<Result> IndexUserAsync(UserSearchDocument document, CancellationToken ct = default);

    Task<Result> DeleteUserAsync(string userId, CancellationToken ct = default);

    Task<Result> UpdateUserAsync(UserSearchDocument document, CancellationToken ct = default);
}

