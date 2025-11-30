using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed class CompleteProfileCommandHandler
    : ICommandHandler<CompleteProfileCommand, ProfileDto>
{
    private readonly IProfileService _profileService;

    public CompleteProfileCommandHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<Result<ProfileDto>> Handle(
        CompleteProfileCommand request,
        CancellationToken ct)
    {
        return await _profileService.CompleteProfileAsync(
            request.UserId,
            request.Request,
            ct);
    }
}

