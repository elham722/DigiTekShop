using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DigiTekShop.API.Extensions;

public static class ETagExtensions
{
    public static string GenerateETag<T>(this T obj)
    {
        if (obj == null) return string.Empty;

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash)[..16]; 
    }

   
    public static bool IsNotModified(this HttpContext context, string etag)
    {
        var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
        return ifNoneMatch == $"\"{etag}\"";
    }

    
    public static void SetETag(this HttpResponse response, string etag)
    {
        response.Headers.ETag = $"\"{etag}\"";
        response.Headers.CacheControl = "private, max-age=300"; 
    }

    public static IActionResult? CheckETag(this HttpContext context, string etag)
    {
        if (context.IsNotModified(etag))
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        return null;
    }
}
