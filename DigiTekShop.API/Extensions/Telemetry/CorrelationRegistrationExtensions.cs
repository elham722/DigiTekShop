using DigiTekShop.Contracts.Abstractions.Telemetry;
using DigiTekShop.API.Services.Telemetry;

namespace DigiTekShop.API.Extensions.Telemetry;

public static class CorrelationRegistrationExtensions
{
    public static IServiceCollection AddCorrelationContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();          
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        return services;
    }
}