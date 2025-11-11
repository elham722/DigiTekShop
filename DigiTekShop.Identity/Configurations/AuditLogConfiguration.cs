using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Enums.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    // Field length constants
    private const int MaxTargetEntityNameLength = 256;
    private const int MaxTargetEntityIdLength = 256;
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxDeviceIdLength = 128;
    private const int MaxErrorMessageLength = 1024;
    private const int MaxCorrelationFieldLength = 128; // For CorrelationId, RequestId, SessionId

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(al => al.Id);

        // Configure properties
        builder.Property(al => al.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(al => al.ActorId)
            .IsRequired();

        builder.Property(al => al.ActorType)
      .IsRequired()
      .HasConversion<string>()        // enum <-> string
      .HasMaxLength(50)
      .HasDefaultValue(ActorType.User);   // ✅ به‌جای nameof(ActorType.User)


        builder.Property(al => al.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(al => al.TargetEntityName)
            .IsRequired()
            .HasMaxLength(MaxTargetEntityNameLength);

        builder.Property(al => al.TargetEntityId)
            .IsRequired()
            .HasMaxLength(MaxTargetEntityIdLength);

        builder.Property(al => al.OldValueJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(al => al.NewValueJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(al => al.Timestamp)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(al => al.IsSuccess)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(MaxErrorMessageLength)
            .IsRequired(false);

        builder.Property(al => al.Severity)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(AuditSeverity.Info)
            .HasSentinel(AuditSeverity.Trace); // CLR default for enum  


        builder.Property(al => al.IpAddress)
            .HasMaxLength(MaxIpAddressLength)
            .IsRequired(false);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(MaxUserAgentLength)
            .IsRequired(false);

        builder.Property(al => al.DeviceId)
            .HasMaxLength(MaxDeviceIdLength)
            .IsRequired(false);

        // Correlation fields
        builder.Property(al => al.CorrelationId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        builder.Property(al => al.RequestId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        builder.Property(al => al.SessionId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        // Configure indexes - optimized for common query patterns
        // Single column indexes (if needed for filtering)
        builder.HasIndex(al => al.ActorId)
            .HasDatabaseName("IX_AuditLogs_ActorId");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        // Composite indexes for common queries (most important first)
        builder.HasIndex(al => new { al.ActorId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_ActorId_Timestamp");

        builder.HasIndex(al => new { al.TargetEntityName, al.TargetEntityId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Target_Timestamp");

        // Index for queries filtering by target entity + action + time
        builder.HasIndex(al => new { al.TargetEntityName, al.TargetEntityId, al.Action, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Target_Action_Time");

        builder.HasIndex(al => new { al.Action, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Action_Timestamp");

        builder.HasIndex(al => new { al.Severity, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Severity_Timestamp");

        // CorrelationId index for request tracking
        builder.HasIndex(al => al.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");
    }
}