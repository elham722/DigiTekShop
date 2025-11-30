using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.DTOs.Profile;

/// <summary>
/// درخواست آپدیت پروفایل
/// </summary>
public sealed record UpdateMyProfileRequest(
    [property: Required(ErrorMessage = "نام کامل الزامی است.")]
    [property: MinLength(2, ErrorMessage = "نام کامل حداقل 2 کاراکتر باشد.")]
    [property: MaxLength(200, ErrorMessage = "نام کامل حداکثر 200 کاراکتر باشد.")]
    string FullName,

    [property: EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست.")]
    string? Email,

    [property: Phone(ErrorMessage = "فرمت شماره تلفن صحیح نیست.")]
    string? Phone
);

