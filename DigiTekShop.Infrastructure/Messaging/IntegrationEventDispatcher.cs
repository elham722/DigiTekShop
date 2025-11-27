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
                        
                        // Dispatch to all registered handlers for this event
                        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<UserRegisteredIntegrationEvent>>();

                        foreach (var handler in handlers)
                        {
                            try
                            {
                                await handler.HandleAsync(evt, ct);
                                _log.LogInformation("✅ Dispatched UserRegisteredIntegrationEvent to {Handler} for UserId {UserId}", 
                                    handler.GetType().Name, evt.UserId);
                            }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "❌ Handler {Handler} failed for UserRegisteredIntegrationEvent UserId {UserId}", 
                                    handler.GetType().Name, evt.UserId);
                                // Don't rethrow - allow other handlers to process
                            }
                        }
                        break;
                    }
                case "DigiTekShop.Contracts.Integration.Events.Customers.AddCustomerIdIntegrationEvent":
                    {
                        var evt = JsonSerializer.Deserialize<AddCustomerIdIntegrationEvent>(payload)!;
                        using var scope = _sp.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<AddCustomerIdIntegrationEvent>>();
                        await handler.HandleAsync(evt, ct);
                        _log.LogInformation("Dispatched AddCustomerIdIntegrationEvent for UserId {UserId} -> CustomerId {CustomerId}", evt.UserId, evt.CustomerId);
                        break;
                    }
                case "DigiTekShop.Contracts.Integration.Events.Identity.PhoneVerificationIssuedIntegrationEvent":
                {
                    var evt = JsonSerializer.Deserialize<PhoneVerificationIssuedIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PhoneVerificationIssuedIntegrationEvent>>();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            await h.HandleAsync(evt, ct);
                            _log.LogInformation("✅ Dispatched PhoneVerificationIssuedIntegrationEvent to {Handler} for UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "❌ Handler {Handler} failed for PhoneVerificationIssuedIntegrationEvent UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                    }
                    break;
                }
                case "DigiTekShop.Contracts.Integration.Events.Identity.UserUpdatedIntegrationEvent":
                {
                    var evt = JsonSerializer.Deserialize<UserUpdatedIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<UserUpdatedIntegrationEvent>>();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            await h.HandleAsync(evt, ct);
                            _log.LogInformation("✅ Dispatched UserUpdatedIntegrationEvent to {Handler} for UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "❌ Handler {Handler} failed for UserUpdatedIntegrationEvent UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                    }
                    break;
                }
                case "DigiTekShop.Contracts.Integration.Events.Identity.UserLockedIntegrationEvent":
                {
                    var evt = JsonSerializer.Deserialize<UserLockedIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<UserLockedIntegrationEvent>>();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            await h.HandleAsync(evt, ct);
                            _log.LogInformation("✅ Dispatched UserLockedIntegrationEvent to {Handler} for UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "❌ Handler {Handler} failed for UserLockedIntegrationEvent UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                    }
                    break;
                }
                case "DigiTekShop.Contracts.Integration.Events.Identity.UserUnlockedIntegrationEvent":
                {
                    var evt = JsonSerializer.Deserialize<UserUnlockedIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<UserUnlockedIntegrationEvent>>();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            await h.HandleAsync(evt, ct);
                            _log.LogInformation("✅ Dispatched UserUnlockedIntegrationEvent to {Handler} for UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "❌ Handler {Handler} failed for UserUnlockedIntegrationEvent UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                    }
                    break;
                }
                case "DigiTekShop.Contracts.Integration.Events.Identity.UserRolesChangedIntegrationEvent":
                {
                    var evt = JsonSerializer.Deserialize<UserRolesChangedIntegrationEvent>(payload)!;
                    using var scope = _sp.CreateScope();
                    var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<UserRolesChangedIntegrationEvent>>();
                    foreach (var h in handlers)
                    {
                        try
                        {
                            await h.HandleAsync(evt, ct);
                            _log.LogInformation("✅ Dispatched UserRolesChangedIntegrationEvent to {Handler} for UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "❌ Handler {Handler} failed for UserRolesChangedIntegrationEvent UserId {UserId}",
                                h.GetType().Name, evt.UserId);
                        }
                    }
                    break;
                }

                default:
                    _log.LogWarning("No handler for integration event type {Type}", type);
                    break;
            }
        }
    }
}
