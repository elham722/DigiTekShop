namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public abstract class AuditableEntity<TId> : Entity<TId>
    {
        public DateTime CreatedAtUtc { get; protected set; }
        public DateTime? UpdatedAtUtc { get; protected set; }

        protected void TouchCreated(DateTime utc) => CreatedAtUtc = utc;
        protected void TouchUpdated(DateTime utc) => UpdatedAtUtc = utc;
    }
}
