using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Enums.Audit;
using DigiTekShop.SharedKernel.Enums.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    // Field length constants
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxDeviceIdLength = 128;
    private const int MaxResolvedByLength = 256;
    private const int MaxResolutionNotesLength = 2000;
    private const int MaxCorrelationFieldLength = 128; // For CorrelationId, RequestId

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
            .HasConversion<int>()
            .IsRequired();

        builder.Property(se => se.Severity)
            .HasConversion<int>()
            .HasDefaultValue(AuditSeverity.Info)
            .IsRequired();

        builder.Property(se => se.IpAddress)
            .HasMaxLength(MaxIpAddressLength)
            .IsRequired(false);

        builder.Property(se => se.UserAgent)
            .HasMaxLength(MaxUserAgentLength)
            .IsRequired(false);

        builder.Property(se => se.DeviceId)
            .HasMaxLength(MaxDeviceIdLength)
            .IsRequired(false);

        builder.Property(se => se.MetadataJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(se => se.OccurredAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(se => se.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(se => se.ResolvedAt)
            .IsRequired(false);

        builder.Property(se => se.ResolvedBy)
            .HasMaxLength(MaxResolvedByLength)
            .IsRequired(false);

        builder.Property(se => se.ResolutionNotes)
            .HasMaxLength(MaxResolutionNotesLength)
            .IsRequired(false);

        // Correlation fields
        builder.Property(se => se.CorrelationId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        builder.Property(se => se.RequestId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        builder.Property(se => se.AuditLogId)
            .IsRequired(false);

        // Configure indexes - optimized for common query patterns
        // Single column indexes (if needed for filtering)
        builder.HasIndex(se => se.UserId)
            .HasDatabaseName("IX_SecurityEvents_UserId");

        builder.HasIndex(se => se.OccurredAt)
            .HasDatabaseName("IX_SecurityEvents_OccurredAt");

        // Composite indexes for common queries (most important first)
        builder.HasIndex(se => new { se.Type, se.IsResolved, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_Type_Resolved_OccurredAt");

        builder.HasIndex(se => new { se.UserId, se.Type, se.OccurredAt })
            .HasDatabaseName("IX_SecurityEvents_UserId_Type_OccurredAt");

        builder.HasIndex(se => new { se.IsResolved, se.OccurredAt })
            .HasFilter("[IsResolved] = 0")
            .HasDatabaseName("IX_SecurityEvents_Unresolved_OccurredAt");

        // Correlation fields indexes
        builder.HasIndex(se => se.CorrelationId)
            .HasDatabaseName("IX_SecurityEvents_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasIndex(se => se.AuditLogId)
            .HasDatabaseName("IX_SecurityEvents_AuditLogId")
            .HasFilter("[AuditLogId] IS NOT NULL");
    }
}
