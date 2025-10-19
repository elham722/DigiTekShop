using System.Text;
using System.Text.Json;
using DigiTekShop.Contracts.Options.RabbitMq;
using DigiTekShop.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DigiTekShop.Infrastructure.Background;

public sealed class RabbitIntegrationEventConsumer : BackgroundService
{
    private readonly ILogger<RabbitIntegrationEventConsumer> _log;
    private readonly IntegrationEventDispatcher _dispatcher;
    private readonly RabbitMqOptions _opt;

    private IConnection? _conn;
    private IChannel? _ch;
    private string _queue = "digitekshop.integration.q";

    public RabbitIntegrationEventConsumer(
        IOptions<RabbitMqOptions> options,
        IntegrationEventDispatcher dispatcher,
        ILogger<RabbitIntegrationEventConsumer> log)
    {
        _dispatcher = dispatcher;
        _log = log;
        _opt = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            VirtualHost = _opt.VirtualHost,
            UserName = _opt.UserName,
            Password = _opt.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
        if (_opt.UseSsl)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = _opt.HostName, // یا CN گواهی
                AcceptablePolicyErrors = System.Net.Security.SslPolicyErrors.None // فقط برای dev
            };
        }
        _conn = await factory.CreateConnectionAsync();
        _ch = await _conn.CreateChannelAsync();

        // declare exchange/queues (v7: Async API)
        await _ch.ExchangeDeclareAsync(_opt.Exchange, _opt.ExchangeType, durable: _opt.Durable);

        // DLX/DLQ (اختیاری)
        await _ch.ExchangeDeclareAsync("digitekshop.dlx", "fanout", durable: true);
        await _ch.QueueDeclareAsync("digitekshop.dlq", durable: true, exclusive: false, autoDelete: false);
        await _ch.QueueBindAsync("digitekshop.dlq", "digitekshop.dlx", routingKey: "");

        var args = new Dictionary<string, object> { ["x-dead-letter-exchange"] = "digitekshop.dlx" };
        await _ch.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        await _ch.QueueBindAsync(_queue, _opt.Exchange, routingKey: "#");

        // QoS
        await _ch.BasicQosAsync(0, prefetchCount: 10, global: false);

        // مصرف‌کننده async
        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString()!;
                var payload = doc.RootElement.GetProperty("payload").GetRawText();

                await _dispatcher.DispatchAsync(type, payload, stoppingToken);
                await _ch!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[RMQ v7] Consume error. Nack -> DLQ");
                await _ch!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        // در v7 متدِ مصرف هم Async است
        await _ch.BasicConsumeAsync(
            queue: _queue,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _log.LogInformation("[RMQ v7] Consumer started on queue {Queue}", _queue);

        // زنده نگه داشتن سرویس
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_ch is not null) await _ch.CloseAsync(); } catch { }
        try { if (_conn is not null) await _conn.CloseAsync(); } catch { }
        await base.StopAsync(cancellationToken);
    }
}
