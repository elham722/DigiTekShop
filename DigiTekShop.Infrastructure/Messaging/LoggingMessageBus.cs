using Microsoft.Extensions.Logging;
using DigiTekShop.Application.Common.Messaging;

namespace DigiTekShop.Infrastructure.Messaging;

public sealed class LoggingMessageBus : IMessageBus
{
    private readonly ILogger<LoggingMessageBus> _logger;
    public LoggingMessageBus(ILogger<LoggingMessageBus> logger) => _logger = logger;

    public Task PublishAsync(string type, string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[BUS] Publishing {Type}: {Payload}", type, payload);
        return Task.CompletedTask;
    }
}