namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public abstract class VersionedAggregateRoot<TId> : AuditableAggregateRoot<TId>, IVersioned
    {
        public byte[] Version { get; private set; } = Array.Empty<byte>();
    }
}
