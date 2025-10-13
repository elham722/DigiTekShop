using DigiTekShop.API.Middleware;

namespace DigiTekShop.API.Extensions.RequestLogging
{
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
            => app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
