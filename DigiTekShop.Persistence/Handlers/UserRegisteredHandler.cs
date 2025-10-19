using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DigiTekShop.Contracts.Integration.Events.Identity;
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

        public UserRegisteredHandler(DigiTekShopDbContext db, ILogger<UserRegisteredHandler> log)
        { _db = db; _log = log; }

        public async Task HandleAsync(UserRegisteredIntegrationEvent evt, CancellationToken ct)
        {
            var exists = await _db.Customers.AsNoTracking().AnyAsync(c => c.UserId == evt.UserId, ct);
            if (exists) return;

            var fullName = string.IsNullOrWhiteSpace(evt.FullName) ? evt.Email : evt.FullName;
            var customer = Customer.Register(evt.UserId, fullName, evt.Email, phone: evt.PhoneNumber);

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Customer created for user {UserId}", evt.UserId);
            _log.LogInformation("CustomerRegistered domain event will be processed by ShopOutboxBeforeCommitInterceptor");
        }
    }

}
