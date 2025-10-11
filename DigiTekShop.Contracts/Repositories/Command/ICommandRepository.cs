
using DigiTekShop.Contracts.Repositories.Abstractions;
using DigiTekShop.SharedKernel.DomainShared.Primitives;
using System.Linq.Expressions;

namespace DigiTekShop.Contracts.Repositories.Command;

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