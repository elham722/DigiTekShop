namespace DigiTekShop.Contracts.DTOs.Customer;

public sealed record CustomerView(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    IReadOnlyList<AddressDto> Addresses
);