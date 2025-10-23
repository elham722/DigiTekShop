using System.ComponentModel.DataAnnotations;
using System.Text;
using DigiTekShop.SharedKernel.Exceptions.Validation;

namespace DigiTekShop.SharedKernel.Utilities.Text;

public static class Normalization
{
    public static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();


    public static string NormalizePhoneIranE164(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("شماره موبایل خالی است.");

      
        var s = ToLatinDigits(phone);

        s = StripNonDigits(s);

        if (s.StartsWith("0098")) s = s[4..];
        else if (s.StartsWith("098")) s = s[3..];
        else if (s.StartsWith("98")) s = s[2..];

        if (s.Length == 11 && s.StartsWith("0"))
            s = s[1..];

        if (s.Length != 10 || !s.StartsWith("9"))
            throw new ValidationException("فرمت شماره موبایل معتبر نیست. مثال درست: 0935xxxxxxx");

        return "+98" + s;
    }

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

    public static string? NormalizePhone(string? phone)
        => NormalizePhoneIranE164(phone);

  
    public static string? UserAgent(string? s, int maxLen = 512)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var v = s.Trim();
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string? Ip(string? s, int maxLen = 45)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var v = s.Trim();
        return v.Length <= maxLen ? v : v[..maxLen];
    }

    public static string? DeviceId(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim();
    }

  
    private static string ToLatinDigits(string input)
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

    private static string StripNonDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
            if (char.IsDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }
}
