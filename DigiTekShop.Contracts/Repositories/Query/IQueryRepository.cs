
using System.Linq.Expressions;
using DigiTekShop.SharedKernel.DomainShared.Primitives;

namespace DigiTekShop.Contracts.Repositories.Query;

public interface IQueryRepository<T, TId>
    where T : AggregateRoot<TId>
{
    Task<T?> GetByIdAsync(TId id, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default);

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default);

    Task<bool> ExistsAsync(TId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    // Paging ساده
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int size,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>[]? includes = null,
        CancellationToken ct = default);

    IQueryBuilder<T, TId> CreateQuery();
}