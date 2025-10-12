namespace DigiTekShop.Contracts.DTOs.Customer;

/// <summary>
/// Response DTO for customer details
/// </summary>
public sealed record CustomerResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<AddressResponse> Addresses
);

/// <summary>
/// Response DTO for address details
/// </summary>
public sealed record AddressResponse(
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    bool IsDefault
);

/// <summary>
/// Response DTO for customer registration
/// </summary>
public sealed record CustomerRegistrationResponse(
    Guid CustomerId,
    string FullName,
    string Email,
    DateTime CreatedAt
);

/// <summary>
/// Response DTO for customer list (with pagination)
/// </summary>
public sealed record CustomerListItemResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    DateTime CreatedAt,
    int AddressCount
);
