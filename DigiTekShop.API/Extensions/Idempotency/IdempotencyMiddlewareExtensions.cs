using DigiTekShop.API.Middleware;

namespace DigiTekShop.API.Extensions.Idempotency
{
    public static class IdempotencyMiddlewareExtensions
    {
        public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}
