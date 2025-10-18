using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using DigiTekShop.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DigiTekShop.Infrastructure.Background
{
    public sealed class ShopIntegrationEventConsumer : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IntegrationEventDispatcher _dispatcher;
        private readonly ILogger<ShopIntegrationEventConsumer> _log;

        public ShopIntegrationEventConsumer(IConnectionMultiplexer redis, IntegrationEventDispatcher dispatcher, ILogger<ShopIntegrationEventConsumer> log)
        {
            _redis = redis; _dispatcher = dispatcher; _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var sub = _redis.GetSubscriber();
            await sub.SubscribeAsync("integration-events", (channel, message) =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var json = message.ToString();       // رفع ابهام overload
                        using var doc = JsonDocument.Parse(json);

                        var type = doc.RootElement.GetProperty("type").GetString()!;
                        var payloadElem = doc.RootElement.GetProperty("payload");
                        var payload = payloadElem.GetRawText();  // ✅ حالا raw JSON object است، نه string
                        await _dispatcher.DispatchAsync(type, payload, ct);

                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Error dispatching integration event");
                    }
                }, ct);

                // ⚠️ هیچ چیزی برنگردون
            });

            // نگه داشتن سرویس
            while (!ct.IsCancellationRequested)
                await Task.Delay(1000, ct);
        }
    }

}
