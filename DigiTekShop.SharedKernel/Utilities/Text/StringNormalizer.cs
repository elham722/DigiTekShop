namespace DigiTekShop.SharedKernel.Utilities.Text;

public static class StringNormalizer
{
   
    public static string? NormalizeAndTruncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return null;

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

