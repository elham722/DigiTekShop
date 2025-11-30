using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Queries.GetProfileStatus;

public sealed class GetProfileStatusQueryHandler
    : IQueryHandler<GetProfileStatusQuery, ProfileCompletionStatus>
{
    private readonly IProfileService _profileService;

    public GetProfileStatusQueryHandler(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<Result<ProfileCompletionStatus>> Handle(
        GetProfileStatusQuery request,
        CancellationToken ct)
    {
        return await _profileService.GetCompletionStatusAsync(request.UserId, ct);
    }
}

