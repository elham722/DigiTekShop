namespace DigiTekShop.Contracts.DTOs.Customer;

public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool IsDefault = false
);