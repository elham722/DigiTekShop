using DigiTekShop.Domain.Customer.Events;
using DigiTekShop.Domain.Customer.ValueObjects;
using DigiTekShop.SharedKernel.DomainShared.Primitives;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Domain.Customer.Entities;

public sealed class Customer : VersionedAggregateRoot<CustomerId>
{
    
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;


    private Customer() { } 
    private Customer(CustomerId id, Guid userId, string fullName, string email, string? phone)
    {
        Id = id;

        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(fullName, nameof(fullName));
        Guard.AgainstEmail(email, nameof(email));

        UserId = userId;
        FullName = fullName.Trim();
        Email = email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        RaiseDomainEvent(new CustomerRegistered(Id.Value, UserId, Email));

        EnsureInvariants();
    }

    public static Customer Register(Guid userId, string fullName, string email, string? phone = null)
        => new(CustomerId.New(), userId, fullName, email, phone);

    public Result ChangeEmail(string newEmail)
    {
        Guard.AgainstEmail(newEmail, nameof(newEmail));

        if (string.Equals(Email, newEmail, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        var old = Email;
        Email = newEmail.Trim();

        RaiseDomainEvent(new CustomerEmailChanged(Id.Value, old, Email));
        EnsureInvariants();
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? phone)
    {
        Guard.AgainstNullOrEmpty(fullName, nameof(fullName));
        FullName = fullName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

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
            address.MakeDefault();
        }
        else if (asDefault)
        {
            for (int i = 0; i < _addresses.Count; i++)
                _addresses[i].UnsetDefault();

            address.MakeDefault();

            var newIndex = _addresses.Count;
         
            RaiseDomainEvent(new CustomerDefaultAddressChanged(Id.Value, newIndex));
        }

        _addresses.Add(address);
        EnsureInvariants();
        return Result.Success();
    }

    public Result SetDefaultAddress(int index)
    {
        if (index < 0 || index >= _addresses.Count)
            return Result.Failure("Address index is out of range.", "CUSTOMER.ADDRESS_INDEX_RANGE");

        for (int i = 0; i < _addresses.Count; i++)
            _addresses[i].UnsetDefault();

        _addresses[index].MakeDefault();

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
        
        Guard.AgainstEmail(Email, nameof(Email));

       
        if (_addresses.Count > 0 && _addresses.Count(a => a.IsDefault) != 1)
            throw new DomainValidationException(
                new[] { "Exactly one default address is required when addresses exist." },
                "Addresses",
                _addresses.Count);

      
        if (FullName.Length is < 2 or > 200)
            throw new DomainValidationException(new[] { "FullName must be between 2 and 200 characters." }, nameof(FullName), FullName.Length);

      
        if (Phone is { Length: > 0 } && Phone.Length > 30)
            throw new DomainValidationException(new[] { "Phone is too long." }, nameof(Phone), Phone.Length);
    }
}
