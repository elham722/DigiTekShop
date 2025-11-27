namespace DigiTekShop.Contracts.Abstractions.Search;

public interface IUserSearchIndexingService
{
    Task<Result> ReindexAllUsersAsync(CancellationToken ct = default);

    Task<Result> IndexUserByIdAsync(Guid userId, CancellationToken ct = default);
}

