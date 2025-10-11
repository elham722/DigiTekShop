
using System.Linq.Expressions;
using DigiTekShop.SharedKernel.DomainShared.Primitives;

namespace DigiTekShop.Contracts.Repositories.Query;

public interface IQueryBuilder<T, TId>
    where T : AggregateRoot<TId>
{
    IQueryBuilder<T, TId> Where(Expression<Func<T, bool>> predicate);
    IQueryBuilder<T, TId> Include(Expression<Func<T, object>> include);
    IQueryBuilder<T, TId> OrderBy(Expression<Func<T, object>> orderBy, bool ascending = true);
    IQueryBuilder<T, TId> Page(int page, int size);

    Task<T?> FirstOrDefaultAsync(CancellationToken ct = default);
    Task<List<T>> ToListAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}