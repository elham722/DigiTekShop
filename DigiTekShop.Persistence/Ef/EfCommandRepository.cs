
using DigiTekShop.Contracts.Repositories.Abstractions;
using DigiTekShop.Contracts.Repositories.Command;
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Primitives;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DigiTekShop.Persistence.Ef;

public sealed class EfCommandRepository<T, TId> : ICommandRepository<T, TId>
    where T : AggregateRoot<TId>
{
    private readonly DigiTekShopDbContext _ctx;
    private readonly DbSet<T> _set;

    public EfCommandRepository(DigiTekShopDbContext ctx) { _ctx = ctx; _set = ctx.Set<T>(); }

    public Task AddAsync(T entity, CancellationToken ct = default)
    { _set.Add(entity); return Task.CompletedTask; }

    public Task AddRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default)
    { _set.AddRange(entities); return Task.CompletedTask; }

    public Task UpdateAsync(T entity,  CancellationToken ct = default)
    { _set.Update(entity); return Task.CompletedTask; }

    public Task UpdateRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default)
    { _set.UpdateRange(entities); return Task.CompletedTask; }

    public Task DeleteAsync(T entity, IAuditContext? audit = null, CancellationToken ct = default)
    { _set.Remove(entity); return Task.CompletedTask; }

    public Task DeleteRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default)
    { _set.RemoveRange(entities); return Task.CompletedTask; }

    public async Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, IAuditContext? audit = null, CancellationToken ct = default)
    {
        var items = await _set.Where(predicate).ToListAsync(ct);
        _set.RemoveRange(items);
        return items.Count;
    }
}