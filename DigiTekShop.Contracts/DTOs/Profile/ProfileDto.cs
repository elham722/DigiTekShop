namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// اطلاعات پروفایل کاربر
/// </summary>
public sealed record ProfileDto
{
    /// <summary>
    /// شناسه کاربر
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// شناسه مشتری
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// نام کامل
    /// </summary>
    public string? FullName { get; init; }

    /// <summary>
    /// شماره موبایل
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// ایمیل
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// آیا پروفایل کامل است؟
    /// </summary>
    public required bool IsProfileComplete { get; init; }

    /// <summary>
    /// تاریخ ایجاد حساب
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}

