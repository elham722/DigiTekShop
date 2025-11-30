namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// درخواست تکمیل پروفایل
/// </summary>
public sealed record CompleteProfileRequest
{
    /// <summary>
    /// نام کامل (نام و نام خانوادگی)
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// ایمیل (اختیاری)
    /// </summary>
    public string? Email { get; init; }
}

