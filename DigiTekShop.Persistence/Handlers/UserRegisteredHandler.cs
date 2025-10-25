using DigiTekShop.Contracts.Integration.Events.Identity;
using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Persistence.Context;
using DigiTekShop.SharedKernel.DomainShared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Persistence.Handlers;

public sealed class UserRegisteredHandler
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    private readonly DigiTekShopDbContext _db;
    private readonly ILogger<UserRegisteredHandler> _log;

    public UserRegisteredHandler(DigiTekShopDbContext db, ILogger<UserRegisteredHandler> log)
    { _db = db; _log = log; }

    public async Task HandleAsync(UserRegisteredIntegrationEvent evt, CancellationToken ct)
    {
        // Idempotency: اگر قبلاً برای این User مشتری ساخته‌ایم، هیچ کاری نکن
        var exists = await _db.Customers.AsNoTracking()
            .AnyAsync(c => c.UserId == evt.UserId, ct);
        if (exists)
        {
            _log.LogInformation("Customer already exists for user {UserId}, skip.", evt.UserId);
            return;
        }

        // Fallback chain برای نام:
        string? name = evt.FullName;
        if (string.IsNullOrWhiteSpace(name))
            name = evt.Email;
        if (string.IsNullOrWhiteSpace(name))
            name = evt.PhoneNumber;
        if (string.IsNullOrWhiteSpace(name))
            name = $"user-{evt.UserId:N}";

        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = "کاربر دیجی‌تک"; // آخرین fallback

        // ساخت Customer (Domain validation دیگر خطا نمی‌دهد)
        var customer = Customer.Register(
            userId: evt.UserId,
            fullName: name!,
            email: string.IsNullOrWhiteSpace(evt.Email) ? null : evt.Email,
            phone: string.IsNullOrWhiteSpace(evt.PhoneNumber) ? null : evt.PhoneNumber,
            correlationId: evt.CorrelationId
        );

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("✅ Customer created for user {UserId} (Name='{Name}') Corr={CorrelationId}",
            evt.UserId, name, evt.CorrelationId);
        _log.LogInformation("CustomerRegistered domain event will be picked by ShopOutboxBeforeCommitInterceptor.");
    }
}
