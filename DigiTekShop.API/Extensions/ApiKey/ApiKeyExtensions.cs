using DigiTekShop.API.Middleware;
using DigiTekShop.API.Security;
using Microsoft.Extensions.Options;

namespace DigiTekShop.API.Extensions.ApiKey;

public static class ApiKeyExtensions
{
    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ApiKeyOptions>(config.GetSection("ApiKey"));
        services.AddSingleton<IValidateOptions<ApiKeyOptions>, ApiKeyOptionsValidator>();
        return services;
    }

    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
        => app.UseMiddleware<ApiKeyMiddleware>();
}

