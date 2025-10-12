using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;

namespace DigiTekShop.Contracts.Abstractions.Repositories.Common.Command;

public interface ICommandRepository<T, TId>
    where T : AggregateRoot<TId>
{
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default);
    Task DeleteAsync(T entity, IAuditContext? audit = null, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, IAuditContext? audit = null, CancellationToken ct = default);

    Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, IAuditContext? audit = null, CancellationToken ct = default);
}