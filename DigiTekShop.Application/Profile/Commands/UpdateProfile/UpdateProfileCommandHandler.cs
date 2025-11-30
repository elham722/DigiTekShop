using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler
    : ICommandHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IProfileService _profileService;

    public UpdateProfileCommandHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<Result<ProfileDto>> Handle(
        UpdateProfileCommand request,
        CancellationToken ct)
    {
        return await _profileService.UpdateProfileAsync(
            request.UserId,
            request.Request,
            ct);
    }
}

