using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Search;

public sealed class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchHealthCheck> _logger;

    public ElasticsearchHealthCheck(
        ElasticsearchClient client,
        ILogger<ElasticsearchHealthCheck> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.PingAsync(cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Elasticsearch health: ping failed. {Error}", response.DebugInformation);
                return HealthCheckResult.Unhealthy("Elasticsearch ping failed", 
                    data: new Dictionary<string, object> { ["error"] = response.DebugInformation });
            }

            var clusterHealth = await _client.Cluster.HealthAsync(cancellationToken);
            var status = clusterHealth.IsValidResponse 
                ? clusterHealth.Status.ToString() 
                : "unknown";

            var data = new Dictionary<string, object>
            {
                ["status"] = status,
                ["cluster_name"] = clusterHealth.ClusterName ?? "unknown"
            };

            return HealthCheckResult.Healthy("Elasticsearch is healthy", data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Elasticsearch health check cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch health check failed");
            return HealthCheckResult.Unhealthy("Elasticsearch connection failed", ex);
        }
    }
}

