using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler
    : IQueryHandler<GetProfileQuery, ProfileDto>
{
    private readonly IProfileService _profileService;

    public GetProfileQueryHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<Result<ProfileDto>> Handle(
        GetProfileQuery request,
        CancellationToken ct)
    {
        return await _profileService.GetProfileAsync(request.UserId, ct);
    }
}

