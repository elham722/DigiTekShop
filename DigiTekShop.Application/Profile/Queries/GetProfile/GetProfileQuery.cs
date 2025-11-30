using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IQuery<ProfileDto>;

