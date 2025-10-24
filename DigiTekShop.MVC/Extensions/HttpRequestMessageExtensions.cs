namespace DigiTekShop.MVC.Extensions;
internal static class HttpRequestMessageExtensions
{
    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage req, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy headers
        foreach (var h in req.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);

        // Copy content (buffer if necessary)
        if (req.Content is null)
            return clone;

        // اگر محتوا از جنس StringContent/ByteArrayContent باشد، دوباره می‌شود استفاده کرد،
        // اما برای اطمینان همه را بافر کنیم:
        var bytes = await req.Content.ReadAsByteArrayAsync(ct);
        var copy = new ByteArrayContent(bytes);

        // کپی هدرهای محتوا
        foreach (var h in req.Content.Headers)
            copy.Headers.TryAddWithoutValidation(h.Key, h.Value);

        clone.Content = copy;
        return clone;
    }
}

