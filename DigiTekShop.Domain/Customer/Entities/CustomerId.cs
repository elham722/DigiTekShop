using DigiTekShop.SharedKernel.DomainShared.Primitives;

namespace DigiTekShop.Domain.Customer.Entities
{
    public sealed record CustomerId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static CustomerId New() => new(Guid.NewGuid());
    }
}