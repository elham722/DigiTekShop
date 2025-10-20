using System.Net.Sockets;
using System.Net;

namespace DigiTekShop.SharedKernel.Utilities.Security;

public static class SensitiveDataMasker
{
    public static string? MaskIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;

        if (IPAddress.TryParse(ip, out var parsed))
        {
            if (parsed.AddressFamily == AddressFamily.InterNetwork) 
            {
                var parts = ip.Split('.');
                return parts.Length == 4 ? $"{parts[0]}.{parts[1]}.***.***" : "***";
            }
            else 
            {
                var s = ip;
                return s.Length > 10 ? s[..10] + "…" : "****";
            }
        }

       
        var first = ip.Split(',').FirstOrDefault()?.Trim();
        return first?.Length > 3 ? first[..^3] + "***" : "***";
    }

    public static string? MaskUserAgent(string? ua, int keep = 50)
        => string.IsNullOrWhiteSpace(ua) ? null : ua.Length > keep ? ua[..keep] + "..." : ua;

    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return email;
        var at = email.IndexOf('@');
        if (at <= 0) return email;
        var left = email[..at];
        var right = email[(at + 1)..];
        var maskedLeft = left.Length <= 1 ? "*" : $"{left[0]}***";
        return $"{maskedLeft}@{right}";
    }

    public static string? MaskPhone(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "***";
        var p = raw.Trim();
        if (p.Length <= 4) return new string('*', p.Length);
        return $"{new string('*', p.Length - 4)}{p[^4..]}";
    }
}
