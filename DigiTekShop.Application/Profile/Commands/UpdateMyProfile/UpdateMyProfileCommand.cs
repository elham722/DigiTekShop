namespace DigiTekShop.Application.Profile.Commands.UpdateMyProfile;

/// <summary>
/// کامند آپدیت پروفایل کاربر
/// </summary>
public sealed record UpdateMyProfileCommand(
    Guid UserId,
    string FullName,
    string? Email,
    string? Phone
) : ICommand;

