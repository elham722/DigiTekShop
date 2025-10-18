﻿using DigiTekShop.SharedKernel.DomainShared.Events;

namespace DigiTekShop.Identity.Events
{
    public sealed record UserRegisteredDomainEvent(
        Guid UserId,
        string Email,
        string? FullName,
        DateTimeOffset OccurredOn,
        string? CorrelationId = null
    ) : DomainEvent;
}
