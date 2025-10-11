namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditable
    {
        public DateTimeOffset CreatedAtUtc { get; protected set; }
        public DateTimeOffset? UpdatedAtUtc { get; protected set; }

        protected void TouchCreated(DateTime utc) => CreatedAtUtc = utc;
        protected void TouchUpdated(DateTime utc) => UpdatedAtUtc = utc;
    }
}
