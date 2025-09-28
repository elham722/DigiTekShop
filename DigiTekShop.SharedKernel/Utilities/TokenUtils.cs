using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.SharedKernel.Utilities
{
    public static class TokenUtils
    {
        public static string GenerateRandomBase64UrlToken(int size = 64)
        {
            var randomBytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return WebEncoders.Base64UrlEncode(randomBytes);
        }

        public static string ComputeHmacSha256(string input, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return WebEncoders.Base64UrlEncode(hash);
        }
    }
}
