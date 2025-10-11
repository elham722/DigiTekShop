using MediatR;
using DigiTekShop.SharedKernel.DomainShared;

namespace DigiTekShop.Infrastructure.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent Event) : INotification
    where TDomainEvent : IDomainEvent;