using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    // Field length constants
    private const int MaxLoginNameLength = 256;
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxDeviceIdLength = 128;
    private const int MaxCorrelationFieldLength = 128; // For CorrelationId, RequestId

    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");

        builder.HasKey(la => la.Id);

        builder.Property(la => la.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(la => la.UserId)
            .IsRequired(false);

        builder.Property(la => la.LoginNameOrEmail)
            .HasMaxLength(MaxLoginNameLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(la => la.LoginNameOrEmailNormalized)
            .HasMaxLength(MaxLoginNameLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(la => la.AttemptedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(la => la.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(la => la.IpAddress)
            .HasMaxLength(MaxIpAddressLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(la => la.UserAgent)
            .HasMaxLength(MaxUserAgentLength)
            .IsRequired(false);

        builder.Property(la => la.DeviceId)
            .HasMaxLength(MaxDeviceIdLength)
            .IsRequired(false);

        // Correlation fields
        builder.Property(la => la.CorrelationId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);

        builder.Property(la => la.RequestId)
            .HasMaxLength(MaxCorrelationFieldLength)
            .IsRequired(false);


        // Configure indexes - optimized for brute-force detection and security analysis
        // Single column indexes (if needed for filtering)
        builder.HasIndex(la => la.UserId)
            .HasDatabaseName("IX_LoginAttempts_UserId");

        builder.HasIndex(la => la.AttemptedAt)
            .HasDatabaseName("IX_LoginAttempts_AttemptedAt");

        // Composite indexes for common queries (most important first)
        // For brute-force detection: Failed attempts by IP + time
        builder.HasIndex(la => new { la.IpAddress, la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Ip_Status_Time");

        // For brute-force detection: Failed attempts by normalized login + time
        builder.HasIndex(la => new { la.LoginNameOrEmailNormalized, la.Status, la.AttemptedAt })
            .HasFilter("[LoginNameOrEmailNormalized] IS NOT NULL")
            .HasDatabaseName("IX_LoginAttempts_Login_Status_Time");

        // For user history queries
        builder.HasIndex(la => new { la.UserId, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_UserId_Time");

        // For filtering failed attempts by time (brute-force detection)
        builder.HasIndex(la => new { la.Status, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Status_Time");

        // Correlation fields indexes
        builder.HasIndex(la => la.CorrelationId)
            .HasDatabaseName("IX_LoginAttempts_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasIndex(la => new { la.IpAddress, la.AttemptedAt })
    .HasDatabaseName("IX_LoginAttempts_Ip_Fail_Time")
    .HasFilter($"[IpAddress] IS NOT NULL AND [Status] <> {(int)LoginStatus.Success}");

        builder.HasIndex(la => new { la.LoginNameOrEmailNormalized, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_LoginNorm_Fail_Time")
            .HasFilter($"[LoginNameOrEmailNormalized] IS NOT NULL AND [Status] <> {(int)LoginStatus.Success}");

    }
}