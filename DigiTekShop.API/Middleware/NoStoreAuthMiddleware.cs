
using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

namespace DigiTekShop.API.Middleware;

public sealed class NoStoreAuthMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Regex _sensitivePaths = new(
        @"^/api/v\d+/(auth|registration|password|twofactor)/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public NoStoreAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        var path = context.Request.Path.Value ?? string.Empty;
        if (_sensitivePaths.IsMatch(path))
        {
            var headers = context.Response.GetTypedHeaders();

            
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
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