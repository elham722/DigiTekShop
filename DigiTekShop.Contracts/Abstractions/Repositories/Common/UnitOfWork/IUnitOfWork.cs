namespace DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        Task<int> SaveChangesWithOutboxAsync(CancellationToken ct = default);

        Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

        Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
    }
}