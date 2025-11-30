namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// آدرس کاربر
/// </summary>
public sealed record MyAddressDto(
    int Id,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool IsDefault
);

