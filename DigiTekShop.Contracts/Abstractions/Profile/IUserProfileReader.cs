namespace DigiTekShop.Contracts.Abstractions.Profile;

/// <summary>
/// خواندن اطلاعات پروفایل از Identity
/// پیاده‌سازی در لایه Identity
/// </summary>
public interface IUserProfileReader
{
    /// <summary>
    /// دریافت اطلاعات پایه کاربر برای پروفایل
    /// </summary>
    Task<UserProfileData?> GetUserDataAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// آپدیت CustomerId کاربر
    /// </summary>
    Task<bool> SetCustomerIdAsync(Guid userId, Guid customerId, CancellationToken ct = default);
}

/// <summary>
/// داده‌های پایه کاربر برای پروفایل
/// </summary>
public sealed record UserProfileData
{
    public required Guid UserId { get; init; }
    public Guid? CustomerId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
}

