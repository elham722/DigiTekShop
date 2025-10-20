namespace DigiTekShop.SharedKernel.Utilities.Text;

public static class Normalization
{
    public static string? LoginKey(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();

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
        var v = s.Trim();
        return v;
    }
}