using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.EntityFrameworkCore.Storage;

namespace DigiTekShop.Persistence.Ef;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DigiTekShopDbContext _db;
    private readonly IDomainEventPublisher _publisher;
    private IDbContextTransaction? _tx;

    public EfUnitOfWork(DigiTekShopDbContext db, IDomainEventPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _tx = await _db.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is not null) await _tx.CommitAsync(ct);
        _tx?.Dispose();
        _tx = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is not null) await _tx.RollbackAsync(ct);
        _tx?.Dispose();
        _tx = null;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task DispatchDomainEventsAsync(CancellationToken ct = default)
    {
        // تمام Aggregateهایی که رویداد دارند را پیدا کن
        var aggregates = _db.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        var allEvents = new List<IDomainEvent>();
        foreach (var agg in aggregates)
            allEvents.AddRange(agg.PullDomainEvents());

        if (allEvents.Count == 0) return;

        // انتشار (در این فاز: MediatR از طریق Publisher)
        await _publisher.PublishAsync(allEvents, ct);
    }
}