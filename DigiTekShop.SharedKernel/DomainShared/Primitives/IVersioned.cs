namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public interface IVersioned
    {
        byte[] Version { get; }
    }
}
