using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
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
            .HasMaxLength(50)
            .HasDefaultValue("User");

        builder.Property(al => al.Action)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(al => al.TargetEntityName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(al => al.TargetEntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(al => al.OldValueJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(al => al.NewValueJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(al => al.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(al => al.IsSuccess)
            .IsRequired();

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(al => al.Severity)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(al => al.DeviceId)
            .HasMaxLength(256)
            .IsRequired(false);

        // Configure indexes
        builder.HasIndex(al => al.ActorId)
            .HasDatabaseName("IX_AuditLogs_ActorId");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(al => al.TargetEntityName)
            .HasDatabaseName("IX_AuditLogs_TargetEntityName");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(al => al.Severity)
            .HasDatabaseName("IX_AuditLogs_Severity");

        builder.HasIndex(al => al.IsSuccess)
            .HasDatabaseName("IX_AuditLogs_IsSuccess");

        // Composite indexes for common queries
        builder.HasIndex(al => new { al.ActorId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_ActorId_Timestamp");

        builder.HasIndex(al => new { al.TargetEntityName, al.TargetEntityId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Entity_Timestamp");

        builder.HasIndex(al => new { al.Action, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Action_Timestamp");

        builder.HasIndex(al => new { al.Severity, al.IsSuccess, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Severity_Success_Timestamp");
    }
}