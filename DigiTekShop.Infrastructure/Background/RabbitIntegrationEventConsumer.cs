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
    private readonly string _queue = "digitekshop.integration.q";

    
    private static readonly TimeSpan _minDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

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
        var delay = _minDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndStartConsumingAsync(stoppingToken);

              
                await WaitUntilDisconnectedAsync(stoppingToken);

              
                delay = _minDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; 
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[RMQ] connect/consume failed. Retrying...");
                await Task.Delay(delay, stoppingToken);
                
                delay = TimeSpan.FromSeconds(Math.Min(_maxDelay.TotalSeconds, delay.TotalSeconds * 2));
            }
        }
    }

    private ConnectionFactory CreateFactory()
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            VirtualHost = _opt.VirtualHost,
            UserName = _opt.UserName,
            Password = _opt.Password,

            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,

            RequestedHeartbeat = TimeSpan.FromSeconds(30),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),

        };

        if (_opt.UseSsl)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = _opt.HostName,
                AcceptablePolicyErrors = System.Net.Security.SslPolicyErrors.None
            };
        }

        return factory;
    }

    private async Task ConnectAndStartConsumingAsync(CancellationToken ct)
    {
      
        await SafeCloseAsync();

        var factory = CreateFactory();

        _log.LogInformation("[RMQ] Connecting to {Host}:{Port} vhost={VHost} ...", _opt.HostName, _opt.Port, _opt.VirtualHost);

        _conn = await factory.CreateConnectionAsync(ct);
        _conn.ConnectionShutdownAsync += OnConnectionShutdownAsync;

        
        _ch = await _conn.CreateChannelAsync(null, ct);

        
        await _ch.ExchangeDeclareAsync(_opt.Exchange, _opt.ExchangeType, durable: _opt.Durable, cancellationToken: ct);

        
        const string dlx = "digitekshop.dlx";
        const string dlq = "digitekshop.dlq";

        await _ch.ExchangeDeclareAsync(dlx, "fanout", durable: true, cancellationToken: ct);
        await _ch.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _ch.QueueBindAsync(dlq, dlx, routingKey: "", cancellationToken: ct);

        var args = new Dictionary<string, object> { ["x-dead-letter-exchange"] = dlx };
        await _ch.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);
        await _ch.QueueBindAsync(_queue, _opt.Exchange, routingKey: "#", cancellationToken: ct);

        
        await _ch.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: ct);

        
        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString()!;
                var payload = doc.RootElement.GetProperty("payload").GetRawText();

                await _dispatcher.DispatchAsync(type, payload, ct);

                if (_ch is not null)
                    await _ch.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // ignore on shutdown
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[RMQ] Consume error. Nack -> DLQ");
                if (_ch is not null)
                    await _ch.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
            }
        };

        await _ch.BasicConsumeAsync(
            queue: _queue,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: ct);

        _log.LogInformation("[RMQ] Consumer started on queue {Queue}", _queue);
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
    {
        _log.LogWarning("[RMQ] Connection shutdown: {ReplyText} ({ReplyCode})", args.ReplyText, args.ReplyCode);
        return Task.CompletedTask;
    }

    private async Task WaitUntilDisconnectedAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_conn is null || !_conn.IsOpen || _ch is null || !_ch.IsOpen)
                break;

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await SafeCloseAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task SafeCloseAsync()
    {
        try
        {
            if (_ch is not null)
            {
                try { await _ch.CloseAsync(); } catch { }
                try { await _ch.DisposeAsync(); } catch { }
            }
        }
        catch { }
        finally { _ch = null; }

        try
        {
            if (_conn is not null)
            {
                try { await _conn.CloseAsync(); } catch { }
                _conn.Dispose();
            }
        }
        catch { }
        finally { _conn = null; }
    }
}
