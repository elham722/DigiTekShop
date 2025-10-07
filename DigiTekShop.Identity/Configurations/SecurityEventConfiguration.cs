using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(se => se.Id);

        // Configure properties
        builder.Property(se => se.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(se => se.UserId)
            .IsRequired(false);

        builder.Property(se => se.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(se => se.IpAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(se => se.UserAgent)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(se => se.DeviceId)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(se => se.MetadataJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(se => se.OccurredAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(se => se.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(se => se.ResolvedAt)
            .IsRequired(false);

        builder.Property(se => se.ResolvedBy)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(se => se.ResolutionNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Configure indexes
        builder.HasIndex(se => se.UserId)
            .HasDatabaseName("IX_SecurityEvents_UserId");

        builder.HasIndex(se => se.Type)
            .HasDatabaseName("IX_SecurityEvents_Type");

        builder.HasIndex(se => se.IpAddress)
            .HasDatabaseName("IX_SecurityEvents_IpAddress");

        builder.HasIndex(se => se.DeviceId)
            .HasDatabaseName("IX_SecurityEvents_DeviceId");

        builder.HasIndex(se => se.OccurredAt)
            .HasDatabaseName("IX_SecurityEvents_OccurredAt");

        builder.HasIndex(se => se.IsResolved)
            .HasDatabaseName("IX_SecurityEvents_IsResolved");

        // Composite indexes for common queries
        builder.HasIndex(se => new { se.UserId, se.Type, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_UserId_Type_OccurredAt");

        builder.HasIndex(se => new { se.IpAddress, se.Type, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_IpAddress_Type_OccurredAt");

        builder.HasIndex(se => new { se.Type, se.IsResolved, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_Type_Resolved_OccurredAt");

        builder.HasIndex(se => new { se.DeviceId, se.Type, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_DeviceId_Type_OccurredAt");

        // Index for unresolved high-severity events
        builder.HasIndex(se => new { se.IsResolved, se.OccurredAt })
            .HasFilter("[IsResolved] = 0")
            .HasDatabaseName("IX_SecurityEvents_Unresolved_OccurredAt");
    }
}
