using DigiTekShop.Contracts.DTOs.Profile;
using DigiTekShop.SharedKernel.Results;

namespace DigiTekShop.Contracts.Abstractions.Profile;

/// <summary>
/// سرویس مدیریت پروفایل کاربر
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// دریافت وضعیت تکمیل پروفایل
    /// </summary>
    Task<Result<ProfileCompletionStatus>> GetCompletionStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت پروفایل کاربر
    /// </summary>
    Task<Result<ProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// تکمیل پروفایل (ساخت Customer)
    /// </summary>
    Task<Result<ProfileDto>> CompleteProfileAsync(
        Guid userId,
        CompleteProfileRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// بروزرسانی پروفایل
    /// </summary>
    Task<Result<ProfileDto>> UpdateProfileAsync(
        Guid userId,
        CompleteProfileRequest request,
        CancellationToken ct = default);
}

