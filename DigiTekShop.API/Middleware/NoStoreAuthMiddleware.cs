using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

namespace DigiTekShop.API.Middleware;

public sealed class NoStoreAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Regex _sensitivePaths;

    public NoStoreAuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        var pattern = config["Security:NoStore:Pattern"]
                      ?? @"^/api/v\d+/(auth|registration|password|twofactor)/";
        _sensitivePaths = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isSensitive = _sensitivePaths.IsMatch(path);

        if (isSensitive)
        {
            context.Response.OnStarting(state =>
            {
                var http = (HttpContext)state;

                // همیشه هدرهای عدم-کش را ست/overwrite کن (به null بودن متکی نباش)
                var typed = http.Response.GetTypedHeaders();
                typed.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoStore = true,
                    NoCache = true,
                    MustRevalidate = true
                };

                http.Response.Headers[HeaderNames.Pragma] = "no-cache";
                http.Response.Headers[HeaderNames.Expires] = "0";

                return Task.CompletedTask;
            }, context);
        }

        return _next(context);
    }
}