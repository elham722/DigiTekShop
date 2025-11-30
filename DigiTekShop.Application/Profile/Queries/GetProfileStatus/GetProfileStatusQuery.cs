using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Queries.GetProfileStatus;

public sealed record GetProfileStatusQuery(Guid UserId) : IQuery<ProfileCompletionStatus>;

