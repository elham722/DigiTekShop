
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DigiTekShop.Contracts.Repositories.Query;
using DigiTekShop.SharedKernel.DomainShared.Primitives;

namespace DigiTekShop.Persistence.Ef;

internal sealed class EfQueryBuilder<T, TId> : IQueryBuilder<T, TId>
    where T : AggregateRoot<TId>
{
    private IQueryable<T> _query;

    public EfQueryBuilder(IQueryable<T> source) => _query = source;

    public IQueryBuilder<T, TId> Where(Expression<Func<T, bool>> predicate)
    { _query = _query.Where(predicate); return this; }

    public IQueryBuilder<T, TId> Include(Expression<Func<T, object>> include)
    { _query = _query.Include(include); return this; }

    public IQueryBuilder<T, TId> OrderBy(Expression<Func<T, object>> orderBy, bool ascending = true)
    { _query = ascending ? _query.OrderBy(orderBy) : _query.OrderByDescending(orderBy); return this; }

    public IQueryBuilder<T, TId> Page(int page, int size)
    { _query = _query.Skip((page - 1) * size).Take(size); return this; }

    public Task<T?> FirstOrDefaultAsync(CancellationToken ct = default) => _query.FirstOrDefaultAsync(ct);
    public Task<List<T>> ToListAsync(CancellationToken ct = default) => _query.ToListAsync(ct);
    public Task<int> CountAsync(CancellationToken ct = default) => _query.CountAsync(ct);
}