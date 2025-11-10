using System.Text.Json;
using System.Text.RegularExpressions;

namespace DigiTekShop.SharedKernel.Utilities.Security;

public static class JsonRedactor
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordHash",
        "passwordSalt",
        "token",
        "accessToken",
        "refreshToken",
        "secret",
        "secretKey",
        "apiKey",
        "apikey",
        "otp",
        "otpCode",
        "verificationCode",
        "cvv",
        "cvv2",
        "cardNumber",
        "creditCard",
        "ssn",
        "socialSecurityNumber",
        "pin",
        "privateKey",
        "privatekey",
        "authorization",
        "authToken"
    };

    private const string RedactedValue = "***redacted***";

    // Static compiled Regex to avoid recompilation on each call
    private static readonly Lazy<Regex> SensitiveFieldRegexLazy = new(() =>
        new Regex(
            @"""(" + string.Join("|", SensitiveKeys.Select(Regex.Escape)) + @")""\s*:\s*""([^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled));

    private static Regex SensitiveFieldRegex => SensitiveFieldRegexLazy.Value;

    public static string? RedactSensitiveFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            // Try regex first (fast path for simple JSON)
            var redacted = SensitiveFieldRegex.Replace(
                json,
                m => $@"""{m.Groups[1].Value}"" : ""{RedactedValue}""");

            // If regex found matches, return early
            if (redacted != json)
                return redacted;

            // For complex JSON, use JsonDocument
            using var doc = JsonDocument.Parse(json);
            var redactedDoc = RedactJsonElement(doc.RootElement);
            return JsonSerializer.Serialize(redactedDoc);
        }
        catch
        {
            // Fallback to regex if JSON parsing fails
            return SensitiveFieldRegex.Replace(
                json,
                m => $@"""{m.Groups[1].Value}"" : ""{RedactedValue}""");
        }
    }

    private static object RedactJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => RedactObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(RedactJsonElement).ToArray(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.GetRawText().Contains('.') 
                ? (object)element.GetDouble() 
                : element.GetInt64(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            _ => element.GetRawText()
        };
    }

    private static Dictionary<string, object> RedactObject(JsonElement obj)
    {
        var result = new Dictionary<string, object>();
        foreach (var prop in obj.EnumerateObject())
        {
            var key = prop.Name;
            var value = prop.Value;

            if (SensitiveKeys.Contains(key))
            {
                result[key] = RedactedValue;
            }
            else if (value.ValueKind == JsonValueKind.Object)
            {
                result[key] = RedactObject(value);
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                result[key] = value.EnumerateArray().Select(RedactJsonElement).ToArray();
            }
            else
            {
                // Use proper CLR types instead of GetRawText() to avoid double serialization
                result[key] = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString()!,
                    JsonValueKind.Number => value.GetRawText().Contains('.') 
                        ? (object)value.GetDouble() 
                        : value.GetInt64(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null!,
                    _ => value.GetRawText()
                };
            }
        }
        return result;
    }
}

