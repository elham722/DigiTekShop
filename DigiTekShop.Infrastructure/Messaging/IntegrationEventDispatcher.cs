using DigiTekShop.Contracts.Integration.Events.Customers;
using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigiTekShop.Infrastructure.Messaging
{
    public sealed class IntegrationEventDispatcher
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<IntegrationEventDispatcher> _log;

        public IntegrationEventDispatcher(IServiceProvider sp, ILogger<IntegrationEventDispatcher> log)
        {
            _sp = sp; _log = log;
        }

        public async Task DispatchAsync(string type, string payload, CancellationToken ct)
        {
            // مثال: full type name را match کن
            switch (type)
            {
                case "DigiTekShop.Contracts.Integration.Events.Identity.UserRegisteredIntegrationEvent":
                    {
                    var evt = JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<UserRegisteredIntegrationEvent>>();
                    await handler.HandleAsync(evt, ct);
                    _log.LogInformation("Dispatched UserRegisteredIntegrationEvent for UserId {UserId}", evt.UserId);
                    break;
                }
                case "DigiTekShop.Contracts.Integration.Events.Customers.CustomerCreatedIntegrationEvent":
                    {
                    var evt = JsonSerializer.Deserialize<AddCustomerIdIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<AddCustomerIdIntegrationEvent>>();
                    await handler.HandleAsync(evt, ct);
                    _log.LogInformation("Dispatched AddCustomerIdIntegrationEvent for UserId {UserId} -> CustomerId {CustomerId}", evt.UserId, evt.CustomerId);
                    break;
                }
                // اینجا بقیه‌ی ایونت‌ها را هم اضافه کن ...
                default:
                    _log.LogWarning("No handler for integration event type {Type}", type);
                    break;
            }
        }
    }
}
