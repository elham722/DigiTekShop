namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public abstract class VersionedEntity<TId> : AuditableEntity<TId>
    {
        public byte[] Version { get; private set; } = Array.Empty<byte>();
    }
}
