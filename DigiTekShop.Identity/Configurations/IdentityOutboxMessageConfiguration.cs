using DigiTekShop.SharedKernel.Enums.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public sealed class IdentityOutboxMessageConfiguration : IEntityTypeConfiguration<IdentityOutboxMessage>
{
    // Field length constants
    private const int MaxTypeLength = 512;
    private const int MaxMessageKeyLength = 256;
    private const int MaxCorrelationIdLength = 128;
    private const int MaxCausationIdLength = 128;
    private const int MaxLockedByLength = 128;
    private const int MaxErrorLength = 1024;

    public void Configure(EntityTypeBuilder<IdentityOutboxMessage> b)
    {
        b.ToTable("IdentityOutboxMessages");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        b.Property(x => x.OccurredAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        b.Property(x => x.Type)
            .HasMaxLength(MaxTypeLength)
            .IsUnicode(false)
            .IsRequired();

        b.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        b.Property(x => x.MessageKey)
            .HasMaxLength(MaxMessageKeyLength)
            .IsUnicode(false)
            .IsRequired(false);

        b.Property(x => x.CorrelationId)
            .HasMaxLength(MaxCorrelationIdLength)
            .IsUnicode(false)
            .IsRequired(false);

        b.Property(x => x.CausationId)
            .HasMaxLength(MaxCausationIdLength)
            .IsUnicode(false)
            .IsRequired(false);

        b.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        b.Property(x => x.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        // Check constraint: Attempts must be non-negative
        b.ToTable(t => t.HasCheckConstraint("CK_Outbox_Attempts_NonNegative", "[Attempts] >= 0"));

        b.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue((int)OutboxStatus.Pending)
            .IsRequired();

        b.Property(x => x.Error)
            .HasMaxLength(MaxErrorLength)
            .IsRequired(false);

        b.Property(x => x.LockedUntilUtc)
            .IsRequired(false);

        b.Property(x => x.LockedBy)
            .HasMaxLength(MaxLockedByLength)
            .IsUnicode(false)
            .IsRequired(false);

        b.Property(x => x.NextRetryUtc)
            .IsRequired(false);

        // Configure indexes - optimized for outbox processing
        // Single column indexes
        b.HasIndex(x => x.OccurredAtUtc)
            .HasDatabaseName("IX_Outbox_OccurredAtUtc");

        // Composite indexes for common queries (most important first)
        // For picking pending messages: Status + NextRetry + OccurredAt
        b.HasIndex(x => new { x.Status, x.NextRetryUtc, x.OccurredAtUtc })
            .HasDatabaseName("IX_Outbox_Status_NextRetry_OccurredAt");

        // For filtering pending/failed messages by time
        b.HasIndex(x => new { x.Status, x.OccurredAtUtc })
            .HasDatabaseName("IX_Outbox_Status_OccurredAtUtc");

        // For idempotency check: MessageKey (unique if not null)
        b.HasIndex(x => x.MessageKey)
            .HasDatabaseName("IX_Outbox_MessageKey")
            .IsUnique()
            .HasFilter("[MessageKey] IS NOT NULL");

        // For correlation tracking
        b.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_Outbox_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        // For lock management
        b.HasIndex(x => x.LockedUntilUtc)
            .HasDatabaseName("IX_Outbox_LockedUntilUtc")
            .HasFilter("[LockedUntilUtc] IS NOT NULL");
    }
}
