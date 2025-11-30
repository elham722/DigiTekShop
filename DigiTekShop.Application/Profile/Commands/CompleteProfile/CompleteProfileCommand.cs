using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed record CompleteProfileCommand(
    Guid UserId,
    CompleteProfileRequest Request
) : ICommand<ProfileDto>;

