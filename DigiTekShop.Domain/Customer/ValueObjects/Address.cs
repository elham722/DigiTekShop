namespace DigiTekShop.Domain.Customer.ValueObjects
{
    public sealed class Address : ValueObject
    {
        public string Line1 { get; }
        public string? Line2 { get; }
        public string City { get; }
        public string? State { get; }
        public string PostalCode { get; }
        public string Country { get; }
        public bool IsDefault { get;  }

        private Address() { } 

        public Address(string line1, string? line2, string city, string? state, string postalCode, string country, bool isDefault = false)
        {
            Guard.AgainstNullOrEmpty(line1, nameof(line1));
            Guard.AgainstNullOrEmpty(city, nameof(city));
            Guard.AgainstNullOrEmpty(postalCode, nameof(postalCode));
            Guard.AgainstNullOrEmpty(country, nameof(country));

            Line1 = line1.Trim();
            Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
            City = city.Trim();
            State = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
            PostalCode = postalCode.Trim();
            Country = country.Trim();
            IsDefault = isDefault;
        }
        public Address WithDefault(bool isDefault) => new(
            Line1, Line2, City, State, PostalCode, Country, isDefault);


        public Address SetAsDefault() => WithDefault(true);
        public Address SetAsNonDefault() => WithDefault(false);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Line1; yield return Line2;
            yield return City; yield return State;
            yield return PostalCode; yield return Country;
        }
    }
}
