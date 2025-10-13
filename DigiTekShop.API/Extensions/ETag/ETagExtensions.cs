using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DigiTekShop.API.Extensions.ETag;

public static class ETagExtensions
{
    public static string GenerateETag<T>(this T obj)
    {
        if (obj is null) return string.Empty;

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash); 
    }

  
    public static bool IsNotModified(this HttpContext context, string etag)
    {
        var inm = context.Request.Headers[HeaderNames.IfNoneMatch];
        if (string.IsNullOrEmpty(inm)) return false;

        
        var target = $"\"{etag}\""; 
        foreach (var raw in inm.ToString().Split(','))
        {
            var token = raw.Trim();
            if (token == "*") return true;              
            if (token.StartsWith("W/")) token = token[2..].Trim(); 
            if (token == target) return true;
        }
        return false;
    }

   
    public static void SetETag(this HttpResponse response, string etag, string? cacheControl = null)
    {
        response.Headers.ETag = $"\"{etag}\"";
        if (!string.IsNullOrWhiteSpace(cacheControl))
        {
            response.Headers.CacheControl = cacheControl;
        }
        else if (!response.Headers.ContainsKey(HeaderNames.CacheControl))
        {
            response.Headers.CacheControl = "private, max-age=300";
        }
    }

   
    public static IActionResult? CheckETag(this HttpContext context, string etag, string? cacheControl = null)
    {
        if (context.IsNotModified(etag))
        {
            context.Response.SetETag(etag, cacheControl);
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }
        return null;
    }
}
