namespace DigiTekShop.Application.Common.Messaging;

public interface IMessageBus
{
    Task PublishAsync(string type, string payload, CancellationToken ct = default);
}