namespace DigiTekShop.MVC.Models.Auth;

/// <summary>
/// درخواست ست‌کردن توکن‌های احراز هویت در کوکی‌های امن
/// </summary>
public sealed record SetAuthCookieRequest
{
    /// <summary>
    /// Access Token دریافتی از API
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh Token برای تمدید خودکار (اختیاری)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// URL برگشت بعد از ورود موفق
    /// </summary>
    public string? ReturnUrl { get; init; }

    /// <summary>
    /// آیا کاربر تازه ثبت‌نام کرده است؟
    /// </summary>
    public bool IsNewUser { get; init; }
}
