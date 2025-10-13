using DigiTekShop.API.Middleware;

namespace DigiTekShop.API.Extensions.Clients
{
    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(
            this IApplicationBuilder app,
            string? headerName = null)
            => app.UseMiddleware<CorrelationIdMiddleware>(headerName);
    }
}
