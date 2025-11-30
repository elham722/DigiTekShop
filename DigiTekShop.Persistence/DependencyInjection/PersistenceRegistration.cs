using DigiTekShop.Application.Common.Events;
using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.Command;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.Query;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Contracts.Abstractions.Telemetry;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.Persistence.Context;
using DigiTekShop.Persistence.Ef;
using DigiTekShop.Persistence.Events;
using DigiTekShop.Persistence.Handlers;
using DigiTekShop.Persistence.Interceptors;
using DigiTekShop.Persistence.Repositories;
using DigiTekShop.Persistence.Repositories.Customer;
using DigiTekShop.SharedKernel.DomainShared.Events;
using DigiTekShop.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigiTekShop.Persistence.DependencyInjection;

public static class PersistenceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. DbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DBConnection")
            ?? throw new InvalidOperationException("Database connection string 'DBConnection' not found");

        services.AddDbContext<DigiTekShopDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
            var mapper = sp.GetRequiredService<IIntegrationEventMapper>();
            var clock = sp.GetRequiredService<IDateTimeProvider>();
            var corr = sp.GetService<ICorrelationContext>(); // اگر ثبت کردی
            opt.AddInterceptors(new ShopOutboxBeforeCommitInterceptor(mapper, clock, corr));
            // Enable detailed errors only in Development
            // opt.EnableDetailedErrors();
            // opt.EnableSensitiveDataLogging();
        });


        // 2. Generic Repositories (Base)
        services.AddScoped(typeof(ICommandRepository<,>), typeof(EfCommandRepository<,>));
        services.AddScoped(typeof(IQueryRepository<,>), typeof(EfQueryRepository<,>));

        // 3. Specific Repositories (Entity-specific)
        services.AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();

        // Profile Repository
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();

               // 4. Unit of Work
               services.AddScoped<IUnitOfWork, EfUnitOfWork>();
               services.AddScoped<IIntegrationEventHandler<UserRegisteredIntegrationEvent>, UserRegisteredHandler>();
               
               // 5. Integration Event Mapper (Register as concrete type, will be used by CompositeMapper)
               services.AddScoped<PersistenceIntegrationEventMapper>();

        return services;
    }
}
