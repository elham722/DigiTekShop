namespace DigiTekShop.Contracts.Abstractions.Repositories.Common.Command;

public interface ICommandRepository<T, TId>
    where T : AggregateRoot<TId>
{
 
    Task AddAsync(T entity, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task UpdateAsync(T entity, CancellationToken ct = default);

    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task DeleteAsync(T entity, CancellationToken ct = default);

    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}