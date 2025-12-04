namespace DigiTekShop.Contracts.DTOs.Profile;

public sealed record CompleteProfileRequest(
    string FullName,
    string? Email
);

