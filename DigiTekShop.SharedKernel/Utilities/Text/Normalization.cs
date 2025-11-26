using System.ComponentModel.DataAnnotations;
using System.Text;
using DigiTekShop.SharedKernel.Exceptions.Validation;

namespace DigiTekShop.SharedKernel.Utilities.Text;

public static class Normalization
{
    // ---------------------------
    // ۱. نرمال‌سازی عمومی رشته‌ها
    // ---------------------------

    /// <summary>
    /// Trim + تبدیل به lower + اگر خالی شد → null
    /// </summary>
    public static string? NormalizeLower(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();

    /// <summary>
    /// فقط Trim، اگر خالی شد → null
    /// </summary>
    public static string? NormalizeTrim(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>
    /// Trim + cut to maxLength، اگر خالی شد → null
    /// </summary>
    public static string? NormalizeAndTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return null;

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    // ---------------------------
    // ۲. موبایل ایران - فرمت E164 (+98912...)
    // ---------------------------

    /// <summary>
    /// نرمال‌سازی شماره موبایل ایران به E164 (مثل +98912xxxxxxx)
    /// برای ورودی نامعتبر ValidationException می‌دهد.
    /// </summary>
    public static string NormalizePhoneIranE164(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("شماره موبایل خالی است.");

        var s = ToLatinDigits(phone);
        s = StripNonDigits(s);

        // پیش‌شماره‌های مختلف
        if (s.StartsWith("0098")) s = s[4..];
        else if (s.StartsWith("098")) s = s[3..];
        else if (s.StartsWith("98")) s = s[2..];

        // 09xxxxxxxxx → 9xxxxxxxxx
        if (s.Length == 11 && s.StartsWith("0"))
            s = s[1..];

        // باید 10 رقم و با 9 شروع شود
        if (s.Length != 10 || !s.StartsWith("9"))
            throw new ValidationException("فرمت شماره موبایل معتبر نیست. مثال درست: 0935xxxxxxx");

        return "+98" + s;
    }

    /// <summary>
    /// نسخه safe برای جاهایی مثل سرچ (بدون استثنا)
    /// </summary>
    public static bool TryNormalizePhoneIranE164(string? phone, out string? e164)
    {
        e164 = null;
        try
        {
            e164 = NormalizePhoneIranE164(phone);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// برای سازگاری؛ فعلاً فقط ایران را پوشش می‌دهیم.
    /// </summary>
    public static string? NormalizePhone(string? phone)
        => NormalizePhoneIranE164(phone);

    // ---------------------------
    // ۳. UserAgent / IP / DeviceId
    // ---------------------------

    public static string? UserAgent(string? s, int maxLen = 512)
    {
        var v = NormalizeTrim(s);
        if (v is null) return null;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string? Ip(string? s, int maxLen = 45)
    {
        var v = NormalizeTrim(s);
        if (v is null) return null;
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string? DeviceId(string? s)
        => NormalizeTrim(s);

    // ---------------------------
    // ۴. Helpers داخلی: اعداد فارسی → لاتین و حذف غیررقمی
    // ---------------------------

    public static string ToLatinDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                // Persian digits
                '\u06F0' => '0',
                '\u06F1' => '1',
                '\u06F2' => '2',
                '\u06F3' => '3',
                '\u06F4' => '4',
                '\u06F5' => '5',
                '\u06F6' => '6',
                '\u06F7' => '7',
                '\u06F8' => '8',
                '\u06F9' => '9',
                // Arabic-Indic digits
                '\u0660' => '0',
                '\u0661' => '1',
                '\u0662' => '2',
                '\u0663' => '3',
                '\u0664' => '4',
                '\u0665' => '5',
                '\u0666' => '6',
                '\u0667' => '7',
                '\u0668' => '8',
                '\u0669' => '9',
                _ => ch
            });
        }
        return sb.ToString();
    }

    public static string StripNonDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            if (char.IsDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }
}
