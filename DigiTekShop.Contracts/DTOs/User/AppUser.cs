namespace DigiTekShop.Contracts.DTOs.User;
public sealed record AppUser(Guid Id, string Email, string? UserName, bool EmailConfirmed);
