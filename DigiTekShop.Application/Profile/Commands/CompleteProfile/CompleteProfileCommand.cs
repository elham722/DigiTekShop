using DigiTekShop.Contracts.DTOs.Auth.Token;

namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed record CompleteProfileCommand(
    Guid UserId,
    string FullName,
    string? Email
) : ICommand<RefreshTokenResponse>;

