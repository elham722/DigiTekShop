namespace DigiTekShop.SharedKernel.DomainShared.Primitives;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
        => obj is ValueObject other && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode()
        => GetEqualityComponents().Aggregate(1, (acc, x) => HashCode.Combine(acc, x?.GetHashCode() ?? 0));
}