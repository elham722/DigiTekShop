namespace DigiTekShop.SharedKernel.Utilities.Text;

public static class Normalization
{
    public static string? LoginKey(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
}