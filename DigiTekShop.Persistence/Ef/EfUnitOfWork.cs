using DigiTekShop.Contracts.Abstractions.Events;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Persistence.Ef;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DigiTekShopDbContext _db;
    private readonly IDomainEventPublisher _publisher;
    private readonly ILogger<EfUnitOfWork> _logger;
    private IDbContextTransaction? _tx;

    public EfUnitOfWork(
        DigiTekShopDbContext db,
        IDomainEventPublisher publisher,
        ILogger<EfUnitOfWork> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _tx = await _db.Database.BeginTransactionAsync(ct);
        _logger.LogDebug("Database transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is null)
        {
            _logger.LogWarning("Attempted to commit null transaction");
            return;
        }

        await _tx.CommitAsync(ct);
        _logger.LogDebug("Database transaction committed successfully");
        
        _tx.Dispose();
        _tx = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_tx is null)
        {
            _logger.LogWarning("Attempted to rollback null transaction");
            return;
        }

        await _tx.RollbackAsync(ct);
        _logger.LogDebug("Database transaction rolled back");
        
        _tx.Dispose();
        _tx = null;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var affectedRows = await _db.SaveChangesAsync(ct);
        _logger.LogDebug("Saved {AffectedRows} rows to database", affectedRows);
        return affectedRows;
    }

    public async Task DispatchDomainEventsAsync(CancellationToken ct = default)
    {
        // Find all Aggregates that have Domain Events
        var aggregates = _db.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .ToList();

        if (aggregates.Count == 0)
        {
            _logger.LogDebug("No aggregates with domain events found");
            return;
        }

        // Collect all events from aggregates
        var allEvents = new List<IDomainEvent>();
        foreach (var agg in aggregates)
        {
            var events = agg.PullDomainEvents();
            allEvents.AddRange(events);
        }

        if (allEvents.Count == 0)
        {
            _logger.LogDebug("No domain events to dispatch");
            return;
        }

        _logger.LogInformation("Dispatching {EventCount} domain events from {AggregateCount} aggregates",
            allEvents.Count, aggregates.Count);

        // Publish all events via MediatR
        await _publisher.PublishAsync(allEvents, ct);
        
        _logger.LogDebug("Domain events dispatched successfully");
    }
}