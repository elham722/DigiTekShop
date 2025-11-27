using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Contracts.Abstractions.Search;

public interface IUserDataProvider
{
    Task<int> GetTotalCountAsync(CancellationToken ct = default);

    Task<IReadOnlyList<UserSearchDocument>> GetUsersBatchAsync(
        int skip,
        int take,
        CancellationToken ct = default);

    Task<UserSearchDocument?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
}

