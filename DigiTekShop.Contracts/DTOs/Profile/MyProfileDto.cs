namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// پروفایل کاربر
/// </summary>
public sealed record MyProfileDto(
    Guid UserId,
    Guid CustomerId,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    IReadOnlyList<MyAddressDto> Addresses
);

