using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    CompleteProfileRequest Request
) : ICommand<ProfileDto>;

