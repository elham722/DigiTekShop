namespace DigiTekShop.Contracts.Customer;

public sealed record RegisterCustomerDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone
);