using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Contracts.Abstractions.Repositories.Abstractions;
using DigiTekShop.Contracts.Abstractions.Repositories.Command;
using DigiTekShop.Contracts.Abstractions.Repositories.Query;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigiTekShop.Persistence.DependencyInjection
{
    public static class PersistenceRegistration
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            services.AddDbContext<DigiTekShopDbContext>(opt =>
                opt.UseSqlServer(configuration.GetConnectionString("DBConnection")));

            // Repositories (EF Generic)
            services.AddScoped(typeof(ICommandRepository<,>), typeof(EfCommandRepository<,>));
            services.AddScoped(typeof(IQueryRepository<,>), typeof(EfQueryRepository<,>));

            // Unit of Work (EF)
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            return services;
        }
    }
}
