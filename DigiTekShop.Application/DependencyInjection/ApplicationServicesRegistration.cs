using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using DigiTekShop.Application.Authorization;
using DigiTekShop.Application.Behaviors;

namespace DigiTekShop.Application.DependencyInjection
{
    public static class ApplicationServicesRegistration
    {
        public static IServiceCollection ConfigureApplicationCore(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

           
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(assembly);
            });
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

      
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            
           
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();


            return services;
        }
    }
}
