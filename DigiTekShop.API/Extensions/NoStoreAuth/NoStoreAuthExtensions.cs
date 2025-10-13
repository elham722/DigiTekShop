using DigiTekShop.API.Middleware;

namespace DigiTekShop.API.Extensions.NoStoreAuth
{
    public static class NoStoreAuthExtensions
    {
        public static IApplicationBuilder UseNoStoreForAuth(this IApplicationBuilder app)
            => app.UseMiddleware<NoStoreAuthMiddleware>();
    }
}
