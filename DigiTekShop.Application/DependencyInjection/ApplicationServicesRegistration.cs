using DigiTekShop.Application.Authorization;
using DigiTekShop.Application.Behaviors;
using DigiTekShop.Application.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DigiTekShop.Application.DependencyInjection
{
    public static class ApplicationServicesRegistration
    {
        public static IServiceCollection ConfigureApplicationCore(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            #region MediatR & Fluentvalidation & Mapper

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            MappingConfig.Register(TypeAdapterConfig.GlobalSettings);

            #endregion


            #region Pipeline Behaviors

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

            #endregion

            return services;
        }
    }
}
