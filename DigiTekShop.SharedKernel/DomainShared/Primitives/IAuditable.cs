namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public interface IAuditable
    {
        DateTimeOffset CreatedAtUtc { get; }
        DateTimeOffset? UpdatedAtUtc { get; }
    }
}
