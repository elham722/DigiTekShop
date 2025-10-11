
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DigiTekShop.Contracts.Repositories.Query;
using DigiTekShop.SharedKernel.DomainShared;
using DigiTekShop.Persistence.Context;

namespace DigiTekShop.Persistence.Ef;

public class EfQueryRepository<T, TId> : IQueryRepository<T, TId>
    where T : AggregateRoot<TId>
{
    private readonly DigiTekShopDbContext _ctx;
    private readonly DbSet<T> _set;
    public EfQueryRepository(DigiTekShopDbContext ctx) { _ctx = ctx; _set = ctx.Set<T>(); }

    public async Task<T?> GetByIdAsync(TId id, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default)
    {
        IQueryable<T> q = _set;
        if (includes != null) foreach (var inc in includes) q = q.Include(inc);
        return await q.FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default)
    {
        IQueryable<T> q = _set;
        if (includes != null) foreach (var inc in includes) q = q.Include(inc);
        return await q.ToListAsync(ct);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default)
    {
        IQueryable<T> q = _set.Where(predicate);
        if (includes != null) foreach (var inc in includes) q = q.Include(inc);
        return await q.ToListAsync(ct);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default)
    {
        IQueryable<T> q = _set.Where(predicate);
        if (includes != null) foreach (var inc in includes) q = q.Include(inc);
        return await q.FirstOrDefaultAsync(ct);
    }

    public Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => _set.AnyAsync(e => e.Id!.Equals(id), ct);

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _set.AnyAsync(predicate, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => _set.CountAsync(ct);

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _set.CountAsync(predicate, ct);

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>[]? includes = null, CancellationToken ct = default)
    {
        IQueryable<T> q = _set;
        if (predicate != null) q = q.Where(predicate);
        if (includes != null) foreach (var inc in includes) q = q.Include(inc);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    public IQueryBuilder<T, TId> CreateQuery() => new EfQueryBuilder<T, TId>(_set.AsQueryable());
}
