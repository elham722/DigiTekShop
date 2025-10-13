using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using DigiTekShop.API.Middleware;
using DigiTekShop.API.Services.Clients;
using DigiTekShop.Contracts.Abstractions.Clients;

namespace DigiTekShop.API.Extensions.Clients;

public static class ClientContextExtensions
{
    public static IServiceCollection AddClientContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentClient, CurrentClient>();

        return services;
    }

    public static IApplicationBuilder UseClientContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<ClientContextMiddleware>();
        return app;
    }
}