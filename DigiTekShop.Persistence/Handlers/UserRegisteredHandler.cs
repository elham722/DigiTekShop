using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DigiTekShop.Application.Common.Messaging;
using DigiTekShop.Contracts.Integration.Events.Customers;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Persistence.Handlers
{
    public sealed class UserRegisteredHandler
        : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
    {
        private readonly DigiTekShopDbContext _db;
        private readonly ILogger<UserRegisteredHandler> _log;
        private readonly IMessageBus _bus;

        public UserRegisteredHandler(DigiTekShopDbContext db, ILogger<UserRegisteredHandler> log, IMessageBus bus)
        { _db = db; _log = log; _bus = bus; }

        public async Task HandleAsync(UserRegisteredIntegrationEvent evt, CancellationToken ct)
        {
            var exists = await _db.Customers.AsNoTracking().AnyAsync(c => c.UserId == evt.UserId, ct);
            if (exists) return;

            var fullName = string.IsNullOrWhiteSpace(evt.FullName) ? evt.Email : evt.FullName;
            var customer = Customer.Register(evt.UserId, fullName, evt.Email, phone: evt.PhoneNumber);

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Customer created for user {UserId}", evt.UserId);

            // رویداد دوم: لینک کردن
            var e2 = new AddCustomerIdIntegrationEvent(
                MessageId: Guid.NewGuid(),
                UserId: evt.UserId,
                CustomerId: customer.Id,
                OccurredOn: DateTimeOffset.UtcNow,
                CorrelationId: evt.CorrelationId,
                CausationId: evt.MessageId.ToString()
            );

            var type = typeof(AddCustomerIdIntegrationEvent).FullName!;
            var payload = JsonSerializer.Serialize(e2);

            await _bus.PublishAsync(type, payload, ct); // 👈 بفرست به MessageBus (Outbox همین لایه)
            _log.LogInformation("Published {Type} for User {UserId} -> Customer {CustomerId}", type, evt.UserId, customer.Id);
        }
    }

}
