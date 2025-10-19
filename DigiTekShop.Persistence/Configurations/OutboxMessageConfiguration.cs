using DigiTekShop.Persistence.Models;
using DigiTekShop.SharedKernel.Enums.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DigiTekShop.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        b.Property(x => x.OccurredAtUtc)
            .IsRequired();

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

        b.Property(x => x.Error)
            .HasColumnType("nvarchar(max)");

        b.HasIndex(x => new { x.Status, x.OccurredAtUtc })
            .HasDatabaseName("IX_Outbox_Status_OccurredAtUtc");

        b.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_Outbox_CorrelationId");
    }
}
