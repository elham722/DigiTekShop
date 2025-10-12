namespace DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);

        Task<int> SaveChangesAsync(CancellationToken ct = default);

        Task DispatchDomainEventsAsync(CancellationToken ct = default);
    }
}