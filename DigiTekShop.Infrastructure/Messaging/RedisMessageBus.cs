using Microsoft.Extensions.Logging;
using DigiTekShop.Application.Common.Messaging;
using StackExchange.Redis;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Messaging;

public sealed class RedisMessageBus : IMessageBus
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMessageBus> _log;
    private const string Channel = "integration-events";

    public RedisMessageBus(IConnectionMultiplexer redis, ILogger<RedisMessageBus> log)
    {
        _redis = redis; _log = log;
    }

    // Infrastructure/Messaging/RedisMessageBus.cs
    public async Task PublishAsync(string type, string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);         // ✅ تبدیل string→JsonDocument
        var envelope = new { type, payload = doc.RootElement }; // ✅ payload به صورت آبجکت JSON
        var json = JsonSerializer.Serialize(envelope);

        await _redis.GetSubscriber().PublishAsync("integration-events", json);
        _log.LogInformation("[BUS:REDIS] Publishing {Type} | PayloadPreview={Preview}",
            type, payload.Length > 200 ? payload.Substring(0, 200) : payload);

    }

}