namespace DigiTekShop.Application.Common.Events;

public sealed class CompositeIntegrationEventMapper : IIntegrationEventMapper
{
    private readonly IEnumerable<IIntegrationEventMapper> _mappers;

    public CompositeIntegrationEventMapper(IEnumerable<IIntegrationEventMapper> mappers)
    {
        _mappers = mappers ?? throw new ArgumentNullException(nameof(mappers));
        // Removed initialization log to avoid console spam
    }

    public IEnumerable<object> MapDomainEventsToIntegrationEvents(IEnumerable<object> domainEvents)
    {
        var eventsList = domainEvents.ToList();
        
        if (eventsList.Count == 0)
            yield break;
            
        Console.WriteLine($"[CompositeMapper] Mapping {eventsList.Count} domain event(s)");
        
        var totalMapped = 0;
        foreach (var mapper in _mappers)
        {
            var mapperName = mapper.GetType().Name;
            var mappedEvents = mapper.MapDomainEventsToIntegrationEvents(eventsList).ToList();
            
            if (mappedEvents.Count > 0)
                Console.WriteLine($"[CompositeMapper]   ↳ {mapperName} → {mappedEvents.Count} integration event(s)");
            
            foreach (var evt in mappedEvents)
            {
                totalMapped++;
                yield return evt;
            }
        }
        
        if (totalMapped > 0)
            Console.WriteLine($"[CompositeMapper] ✅ Total: {totalMapped} integration event(s) mapped");
    }
}

