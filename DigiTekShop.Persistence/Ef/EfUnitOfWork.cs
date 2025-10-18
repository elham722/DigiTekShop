using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using DigiTekShop.Persistence.Context;                // AppDbContext شما
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigiTekShop.Persistence.Ef;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DigiTekShopDbContext _db;          
    private readonly ILogger<EfUnitOfWork> _logger;

    public EfUnitOfWork(
        DigiTekShopDbContext db,
        ILogger<EfUnitOfWork> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        int affected = 0;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                affected = await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                _logger.LogDebug("UoW committed ({Rows} rows).", affected);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return affected;
    }

    public async Task<int> SaveChangesWithOutboxAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        int affected = 0;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // 1) ذخیره تغییرات دامنه
                affected = await _db.SaveChangesAsync(ct);

                // 2) رویدادهای دامنه -> Outbox
               
                // 3) ذخیره Outbox
                await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                _logger.LogDebug("UoW committed with outbox ({Rows} rows).", affected);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return affected;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        T result = default!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                result = await action(ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return result;
    }

   
    private static string GetAggregateId(IDomainEvent e)
        => e.GetType().GetProperties()
            .FirstOrDefault(p => p.Name.EndsWith("Id") && p.PropertyType == typeof(Guid))
            ?.GetValue(e)?.ToString()
           ?? Guid.NewGuid().ToString();

    private static string GetAggregateType(IDomainEvent e)
        => e.GetType().Name.EndsWith("Event")
           ? e.GetType().Name[..^5]
           : e.GetType().Name;
}
