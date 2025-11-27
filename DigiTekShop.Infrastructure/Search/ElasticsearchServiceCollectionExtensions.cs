using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigiTekShop.Infrastructure.Search;

public static class ElasticsearchServiceCollectionExtensions
{
    public static IServiceCollection AddDigiTekElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var url = configuration.GetSection("Elasticsearch")["Url"]
                  ?? throw new InvalidOperationException("Elasticsearch:Url is not configured.");

        var settings = new ElasticsearchClientSettings(new Uri(url))
            .PrettyJson()
            .DefaultFieldNameInferrer(p => p); // اسم پراپرتی‌ها همونی که هست بمونه

        var client = new ElasticsearchClient(settings);

        services.AddSingleton(client);

        return services;
    }
}
