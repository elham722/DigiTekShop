using DigiTekShop.SharedKernel.Enums.Outbox;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigiTekShop.Identity.Configurations;

public sealed class IdentityOutboxMessageConfiguration : IEntityTypeConfiguration<IdentityOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IdentityOutboxMessage> b)
    {
        b.ToTable("IdentityOutboxMessages");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        b.Property(x => x.OccurredAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        b.Property(x => x.Type)
            .HasMaxLength(512)      
            .IsUnicode(false)
            .IsRequired();

        b.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)") 
            .IsRequired();

        b.Property(x => x.CorrelationId)
            .HasMaxLength(100)
            .IsUnicode(false);

        b.Property(x => x.CausationId)
            .HasMaxLength(100)
            .IsUnicode(false);

        b.Property(x => x.ProcessedAtUtc);

        b.Property(x => x.Attempts)
            .HasDefaultValue(0);

        
        var statusConverter = new EnumToStringConverter<OutboxStatus>();
        b.Property(x => x.Status)
            .HasConversion(statusConverter)
            .HasMaxLength(20)
            .HasDefaultValue(OutboxStatus.Pending)
            .IsRequired();

        b.Property(x => x.LockedUntilUtc);
        b.Property(x => x.LockedBy).HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.NextRetryUtc);


        b.Property(x => x.Error)
            .HasColumnType("nvarchar(max)");

        b.HasIndex(x => new { x.Status, x.OccurredAtUtc })
            .HasDatabaseName("IX_Outbox_Status_OccurredAtUtc");

        b.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_Outbox_CorrelationId");

        
        b.HasIndex(x => new { x.Status, x.NextRetryUtc, x.OccurredAtUtc })
            .HasDatabaseName("IX_Outbox_Status_NextRetry_OccurredAt");

        
        b.HasIndex(x => x.LockedUntilUtc)
            .HasDatabaseName("IX_Outbox_LockedUntilUtc");
    }
}
