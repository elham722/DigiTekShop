using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.Options.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DigiTekShop.Infrastructure.Search;

public static class ElasticsearchServiceCollectionExtensions
{
    public static IServiceCollection AddDigiTekElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<ElasticsearchOptions>(configuration.GetSection("Elasticsearch"));

        var options = configuration.GetSection("Elasticsearch").Get<ElasticsearchOptions>()
                      ?? throw new InvalidOperationException("Elasticsearch configuration is not valid.");

        var settings = new ElasticsearchClientSettings(new Uri(options.Url))
            .DefaultFieldNameInferrer(p => p)
            .RequestTimeout(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));

        if (environment.IsDevelopment() && options.PrettyJson)
        {
            settings = settings.PrettyJson();
        }

        var client = new ElasticsearchClient(settings);
        services.AddSingleton(client);

        services.AddScoped<IElasticsearchIndexManager, ElasticsearchIndexManager>();
        services.AddScoped<IUserSearchService, UserSearchService>();
        services.AddScoped<IUserSearchIndexingService, UserSearchIndexingService>();

        // Register Elasticsearch event handlers
        services.AddScoped<DigiTekShop.SharedKernel.DomainShared.Events.IIntegrationEventHandler<DigiTekShop.Contracts.Integration.Events.Identity.UserUpdatedIntegrationEvent>, 
            DigiTekShop.Infrastructure.Search.Handlers.UserUpdatedElasticsearchHandler>();
        services.AddScoped<DigiTekShop.SharedKernel.DomainShared.Events.IIntegrationEventHandler<DigiTekShop.Contracts.Integration.Events.Identity.UserLockedIntegrationEvent>, 
            DigiTekShop.Infrastructure.Search.Handlers.UserLockedElasticsearchHandler>();
        services.AddScoped<DigiTekShop.SharedKernel.DomainShared.Events.IIntegrationEventHandler<DigiTekShop.Contracts.Integration.Events.Identity.UserUnlockedIntegrationEvent>, 
            DigiTekShop.Infrastructure.Search.Handlers.UserUnlockedElasticsearchHandler>();
        services.AddScoped<DigiTekShop.SharedKernel.DomainShared.Events.IIntegrationEventHandler<DigiTekShop.Contracts.Integration.Events.Identity.UserRolesChangedIntegrationEvent>, 
            DigiTekShop.Infrastructure.Search.Handlers.UserRolesChangedElasticsearchHandler>();

        if (options.EnableHealthCheck)
        {
            services.AddHealthChecks()
                .AddCheck<ElasticsearchHealthCheck>(
                    name: "elasticsearch",
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                    tags: new[] { "elasticsearch", "infrastructure" },
                    timeout: TimeSpan.FromSeconds(5));
        }

        return services;
    }
}
