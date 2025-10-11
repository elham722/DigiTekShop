namespace DigiTekShop.SharedKernel.DomainShared.Primitives
{
    public abstract record StronglyTypedId<T>(T Value) where T : notnull
    {
        public override string ToString() => Value.ToString()!;
        public static implicit operator T(StronglyTypedId<T> id) => id.Value;
    }

}
