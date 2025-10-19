namespace DigiTekShop.Contracts.Integration.Events.Customers
{
    public sealed record AddCustomerIdIntegrationEvent(
        Guid MessageId,
        Guid UserId,
        Guid CustomerId,
        DateTimeOffset OccurredOn,
        string? CorrelationId = null,
        string? CausationId = null
    );
}
