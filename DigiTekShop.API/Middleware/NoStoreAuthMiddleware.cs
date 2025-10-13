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

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        var path = context.Request.Path.Value ?? string.Empty;
        if (!_sensitivePaths.IsMatch(path))
            return;

        var headers = context.Response.GetTypedHeaders();
        if (headers.CacheControl is null)
        {
            headers.CacheControl = new CacheControlHeaderValue
            {
                NoStore = true,
                NoCache = true,
                MustRevalidate = true
            };

            
            context.Response.Headers[HeaderNames.Pragma] = "no-cache";
            context.Response.Headers[HeaderNames.Expires] = "0";
        }
    }
}

