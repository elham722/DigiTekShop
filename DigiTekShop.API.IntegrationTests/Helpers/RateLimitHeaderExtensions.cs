namespace DigiTekShop.API.IntegrationTests.Helpers;

/// <summary>
/// Extension Methods برای خواندن هدرهای Rate Limit از HttpResponseMessage
/// </summary>
public static class RateLimitHeaderExtensions
{
    /// <summary>
    /// سعی در خواندن یک هدر خاص از Response (چه Response Headers چه Content Headers)
    /// </summary>
    public static bool TryGetHeader(this HttpResponseMessage response, string name, out string value)
    {
        value = string.Empty;

        // اول Response Headers
        if (response.Headers.TryGetValues(name, out var headerValues))
        {
            value = headerValues.First();
            return true;
        }

        // بعد Content Headers
        if (response.Content?.Headers?.TryGetValues(name, out var contentValues) == true)
        {
            value = contentValues.First();
            return true;
        }

        return false;
    }

    /// <summary>
    /// خواندن یک هدر به صورت int (اگر موجود و معتبر باشد)
    /// </summary>
    public static int? GetIntHeader(this HttpResponseMessage response, string name)
    {
        if (response.TryGetHeader(name, out var value) && int.TryParse(value, out var intValue))
            return intValue;

        return null;
    }

    /// <summary>
    /// خواندن هدر X-RateLimit-Remaining
    /// </summary>
    public static int? GetRateLimitRemaining(this HttpResponseMessage response)
        => response.GetIntHeader("X-RateLimit-Remaining");

    /// <summary>
    /// خواندن هدر X-RateLimit-Limit
    /// </summary>
    public static int? GetRateLimitLimit(this HttpResponseMessage response)
        => response.GetIntHeader("X-RateLimit-Limit");

    /// <summary>
    /// خواندن هدر Retry-After
    /// </summary>
    public static int? GetRetryAfter(this HttpResponseMessage response)
        => response.GetIntHeader("Retry-After");

    /// <summary>
    /// خواندن هدر X-RateLimit-Reset
    /// </summary>
    public static long? GetRateLimitReset(this HttpResponseMessage response)
    {
        if (response.TryGetHeader("X-RateLimit-Reset", out var value) && long.TryParse(value, out var longValue))
            return longValue;

        return null;
    }
}

