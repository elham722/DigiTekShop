using DigiTekShop.Contracts.DTOs.Profile;

namespace DigiTekShop.Application.Profile.Queries.GetMyProfile;

/// <summary>
/// کوئری دریافت پروفایل کاربر جاری
/// </summary>
public sealed record GetMyProfileQuery(Guid UserId) : IQuery<MyProfileDto>;

