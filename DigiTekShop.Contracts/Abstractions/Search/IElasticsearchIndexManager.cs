using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Abstractions.Search;

public interface IElasticsearchIndexManager
{
    Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default);

    Task<Result> CreateIndexAsync(string indexName, CancellationToken ct = default);

    Task<Result> DeleteIndexAsync(string indexName, CancellationToken ct = default);

    Task<Result> RecreateIndexAsync(string indexName, CancellationToken ct = default);
}

