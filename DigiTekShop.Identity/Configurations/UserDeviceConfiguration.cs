using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> b)
    {
        b.ToTable("UserDevices");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(128) // Consistent with other models (LoginAttempt, PhoneVerification, RefreshToken)
            .IsUnicode(false);

        b.Property(x => x.DeviceName)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.DeviceFingerprint)
            .HasMaxLength(256)
            .IsUnicode(false);

        b.Property(x => x.BrowserInfo)
            .HasMaxLength(512)       
            .IsUnicode(false);

        b.Property(x => x.OperatingSystem)
            .HasMaxLength(64);

        b.Property(x => x.LastIp)
            .HasMaxLength(45)        
            .IsUnicode(false);

        b.Property(x => x.FirstSeenUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        b.Property(x => x.LastSeenUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.Property(x => x.TrustedAtUtc).IsRequired(false);
        b.Property(x => x.TrustedUntilUtc).IsRequired(false);
        b.Property(x => x.TrustCount).HasDefaultValue(0);

        b.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        b.HasOne(x => x.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        b.HasQueryFilter(ud => ud.User != null && !ud.User.IsDeleted);

        b.HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName("UX_UserDevices_User_DeviceId");

        b.HasIndex(x => x.UserId).HasDatabaseName("IX_UserDevices_UserId");
        b.HasIndex(x => x.LastSeenUtc).HasDatabaseName("IX_UserDevices_LastSeenUtc");

        b.HasIndex(x => new { x.UserId, x.DeviceFingerprint })
            .IsUnique()
            .HasFilter("[DeviceFingerprint] IS NOT NULL")
            .HasDatabaseName("UX_UserDevices_User_Fingerprint");

        b.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("IX_UserDevices_User_IsActive");

        // Composite index for queries filtering active devices and ordering by LastSeenUtc
        // Helps with queries like "get active devices for user ordered by most recent"
        b.HasIndex(x => new { x.UserId, x.IsActive, x.LastSeenUtc })
            .HasDatabaseName("IX_UserDevices_User_Active_LastSeen");

        // Filtered index for counting trusted devices per user (useful for reporting and limits enforcement)
        // Note: Cannot use non-deterministic functions (SYSUTCDATETIME) in index filter
        // The "active" logic (TrustedUntilUtc > now) is enforced in application layer
        b.HasIndex(x => new { x.UserId, x.TrustedUntilUtc })
            .HasFilter("[TrustedUntilUtc] IS NOT NULL")
            .HasDatabaseName("IX_UserDevices_User_TrustedUntil");

        // Check constraints
        b.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_UserDevices_LastSeen_GTE_FirstSeen",
                "[LastSeenUtc] >= [FirstSeenUtc]");

            tb.HasCheckConstraint(
                "CK_UserDevices_TrustRange",
                "([TrustedUntilUtc] IS NULL AND [TrustedAtUtc] IS NULL) OR ([TrustedUntilUtc] >= [TrustedAtUtc])");
        });
    }
}
