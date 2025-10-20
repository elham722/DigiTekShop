using System.Security.Cryptography;

namespace DigiTekShop.SharedKernel.Utilities.Security;
public static class Hashing
{
    public static string Sha256Base64Url(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
        var b64 = Convert.ToBase64String(bytes);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}