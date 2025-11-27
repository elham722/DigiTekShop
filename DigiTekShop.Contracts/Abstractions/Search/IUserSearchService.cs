using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Contracts.Abstractions.Search;

public interface IUserSearchService
{
    Task<Result<UserSearchResult>> SearchAsync(
        string query,
        int page = 1,
        int pageSize = 10,
        string? status = null,
        CancellationToken ct = default);

    Task<Result> IndexUserAsync(UserSearchDocument document, CancellationToken ct = default);

    Task<Result> DeleteUserAsync(string userId, CancellationToken ct = default);

    Task<Result> UpdateUserAsync(UserSearchDocument document, CancellationToken ct = default);
}

