using System.Text;
using System.Text.Json;
using DigiTekShop.Application.Common.Messaging;
using DigiTekShop.Contracts.Options.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DigiTekShop.Infrastructure.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ILogger<RabbitMqMessageBus> _log;
    private readonly RabbitMqOptions _opt;
    private readonly ConnectionFactory _factory;

    private IConnection? _conn;
    private IChannel? _channel;
    private bool _exchangeDeclared;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public RabbitMqMessageBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBus> log)
    {
        _log = log;
        _opt = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password,
            // در v7 همه‌چیز async است؛ نیازی به DispatchConsumersAsync مثل قبل نیست
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }

    // تا وقتی لازم نشه کانکشن/چنل باز نمی‌کنیم
    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _sync.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            _conn ??= await _factory.CreateConnectionAsync();
            _channel = await _conn.CreateChannelAsync();

            if (!_exchangeDeclared)
            {
                await _channel.ExchangeDeclareAsync(_opt.Exchange, _opt.ExchangeType, durable: _opt.Durable);
                _exchangeDeclared = true;
            }

            return _channel;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task PublishAsync(string type, string payload, CancellationToken ct = default)
    {
        var ch = await GetChannelAsync(ct);

        using var doc = JsonDocument.Parse(payload);
        var envelope = new { type, payload = doc.RootElement };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

        // در v7 به جای IBasicProperties کلاس BasicProperties استفاده می‌شود
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent // persistent
        };

        await ch.BasicPublishAsync(_opt.Exchange, routingKey: type, mandatory: false, basicProperties: props, body: body);
        _log.LogInformation("[BUS:RMQ v7] Published {Type} ({Bytes} bytes)", type, body.Length);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_channel is not null) await _channel.CloseAsync(); } catch { /* ignore */ }
        try { if (_conn is not null) await _conn.CloseAsync(); } catch { /* ignore */ }
        try { if (_channel is IAsyncDisposable ad) await ad.DisposeAsync(); } catch { }
        try { if (_conn is IAsyncDisposable ad2) await ad2.DisposeAsync(); } catch { }
        _sync.Dispose();
    }
}
