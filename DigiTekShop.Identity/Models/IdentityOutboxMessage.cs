using DigiTekShop.SharedKernel.Enums.Outbox;
using DigiTekShop.SharedKernel.Guards;
using DigiTekShop.SharedKernel.Utilities.Text;

namespace DigiTekShop.Identity.Models;

public sealed class IdentityOutboxMessage
{
    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string? MessageKey { get; private set; } // For idempotency
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public OutboxStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? NextRetryUtc { get; private set; }

    private IdentityOutboxMessage() { }

    public static IdentityOutboxMessage Create(
        string type,
        string payload,
        string? messageKey = null,
        string? correlationId = null,
        string? causationId = null)
    {
        Guard.AgainstNullOrEmpty(type, nameof(type));
        Guard.AgainstNullOrEmpty(payload, nameof(payload));

        // Normalize and truncate string fields
        var normalizedType = StringNormalizer.NormalizeAndTruncate(type, 512);
        var normalizedMessageKey = StringNormalizer.NormalizeAndTruncate(messageKey, 256);
        var normalizedCorrelationId = StringNormalizer.NormalizeAndTruncate(correlationId, 128);
        var normalizedCausationId = StringNormalizer.NormalizeAndTruncate(causationId, 128);

        return new IdentityOutboxMessage
        {
            Id = Guid.NewGuid(),
            // OccurredAtUtc will be set by DB via HasDefaultValueSql("SYSUTCDATETIME()")
            Type = normalizedType!,
            Payload = payload,
            MessageKey = normalizedMessageKey,
            CorrelationId = normalizedCorrelationId,
            CausationId = normalizedCausationId,
            Status = OutboxStatus.Pending,
            Attempts = 0
        };
    }

    public void MarkAsProcessing(string lockedBy, TimeSpan ttl)
    {
        Guard.AgainstNullOrEmpty(lockedBy, nameof(lockedBy));

        Status = OutboxStatus.Processing;
        LockedBy = StringNormalizer.NormalizeAndTruncate(lockedBy, 128);
        LockedUntilUtc = DateTimeOffset.UtcNow.Add(ttl);
    }

    public void MarkAsSucceeded()
    {
        Status = OutboxStatus.Succeeded;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
        LockedBy = null;
        LockedUntilUtc = null;
    }

    public void MarkAsFailed(string? error, DateTimeOffset? nextRetryUtc = null)
    {
        Attempts++; // Model increments itself
        Status = nextRetryUtc.HasValue ? OutboxStatus.Pending : OutboxStatus.Failed;
        Error = StringNormalizer.NormalizeAndTruncate(error, 1024);
        NextRetryUtc = nextRetryUtc;
        LockedBy = null;
        LockedUntilUtc = null;
    }
}

