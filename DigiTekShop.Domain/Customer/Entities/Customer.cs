using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Domain.Customer.Entities;

public sealed class Customer : VersionedAggregateRoot<CustomerId>
{
    private readonly List<Address> _addresses = [];
    public IReadOnlyList<Address> Addresses => _addresses;

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = default!;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Customer() { }

    private Customer(CustomerId id, Guid userId, string fullName, string? email, string? phone)
    {
        Id = id;

        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(fullName, nameof(fullName));

        UserId = userId;
        FullName = fullName.Trim();

        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (Email is not null)
            Guard.AgainstEmail(Email, nameof(email));

        Phone = string.IsNullOrWhiteSpace(phone) ? null : Normalization.NormalizePhoneIranE164(phone);

        EnsureInvariants();
    }

    public static Customer Register(Guid userId, string fullName, string? email = null, string? phone = null, string? correlationId = null)
    {
        var customer = new Customer(CustomerId.New(), userId, fullName, email, phone);

        customer.RaiseDomainEvent(new CustomerRegistered(
            CustomerId: customer.Id.Value,
            UserId: userId,
            OccurredOn: DateTimeOffset.UtcNow,
            CorrelationId: correlationId
        ));

        return customer;
    }

    public Result ChangeEmail(string? newEmail)
    {
        string? cleaned = string.IsNullOrWhiteSpace(newEmail) ? null : newEmail.Trim();
        if (cleaned is not null)
            Guard.AgainstEmail(cleaned, nameof(newEmail));

        if (string.Equals(Email, cleaned, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        var old = Email;
        Email = cleaned;

        if (!string.Equals(old, Email, StringComparison.OrdinalIgnoreCase))
            RaiseDomainEvent(new CustomerEmailChanged(Id.Value, old ?? string.Empty, Email ?? string.Empty));

        EnsureInvariants();
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? phone)
    {
        Guard.AgainstNullOrEmpty(fullName, nameof(fullName));
        FullName = fullName.Trim();

        Phone = string.IsNullOrWhiteSpace(phone) ? null : Normalization.NormalizePhoneIranE164(phone);

        EnsureInvariants();
        return Result.Success();
    }

    public Result UpdatePhone(string? phone)
    {
        Phone = string.IsNullOrWhiteSpace(phone) ? null : Normalization.NormalizePhoneIranE164(phone);
        EnsureInvariants();
        return Result.Success();
    }

    public Result AddAddress(Address address, bool asDefault = false)
    {
        Guard.AgainstNull(address, nameof(address));
        const int MaxAddresses = 5;
        if (_addresses.Count >= MaxAddresses)
            return Result.Failure("Maximum of 5 addresses is allowed.", "CUSTOMER.ADDRESS_LIMIT");

        if (_addresses.Count == 0)
        {
            _addresses.Add(address.SetAsDefault());
        }
        else if (asDefault)
        {
            for (int i = 0; i < _addresses.Count; i++)
                _addresses[i] = _addresses[i].SetAsNonDefault();

            _addresses.Add(address.SetAsDefault());
            var newIndex = _addresses.Count - 1;
            RaiseDomainEvent(new CustomerDefaultAddressChanged(Id.Value, newIndex));
        }
        else
        {
            _addresses.Add(address.SetAsNonDefault());
        }

        EnsureInvariants();
        return Result.Success();
    }

    public Result SetDefaultAddress(int index)
    {
        if (index < 0 || index >= _addresses.Count)
            return Result.Failure("Address index is out of range.", "CUSTOMER.ADDRESS_INDEX_RANGE");

        for (int i = 0; i < _addresses.Count; i++)
            _addresses[i] = _addresses[i].SetAsNonDefault();

        _addresses[index] = _addresses[index].SetAsDefault();
        RaiseDomainEvent(new CustomerDefaultAddressChanged(Id.Value, index));
        EnsureInvariants();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Success();
        IsActive = false;
        EnsureInvariants();
        return Result.Success();
    }

    protected override void ValidateState()
    {
        if (Email is not null)
            Guard.AgainstEmail(Email, nameof(Email));

        if (_addresses.Count > 0 && _addresses.Count(a => a.IsDefault) != 1)
            throw new DomainValidationException(
                new[] { "Exactly one default address is required when addresses exist." },
                "Addresses",
                _addresses.Count);

        if (FullName.Length is < 2 or > 200)
            throw new DomainValidationException(new[] { "FullName must be between 2 and 200 characters." }, nameof(FullName), FullName.Length);

        if (Phone is { Length: > 0 })
        {
            if (Phone.Length > 32)
                throw new DomainValidationException(new[] { "Phone is too long." }, nameof(Phone), Phone.Length);

            if (!(Phone.StartsWith("+98") && Phone.Length == 13))
                throw new DomainValidationException(new[] { "Phone must be in E.164 format for Iran (+98XXXXXXXXXX)." }, nameof(Phone), Phone);
        }
    }
}

