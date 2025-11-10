using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class UserMfaConfiguration : IEntityTypeConfiguration<UserMfa>
{
    public void Configure(EntityTypeBuilder<UserMfa> builder)
    {
        builder.ToTable("UserMfa");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SecretKeyEncrypted)
            .IsRequired()
            .HasMaxLength(512) // Base64/encrypted text; increase to 1024 if needed for longer keys
            .IsUnicode(false);

        builder.Property(x => x.IsEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.LastVerifiedAt)
            .IsRequired(false);

        builder.Property(x => x.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.LockedAt)
            .IsRequired(false);

        builder.Property(x => x.LockedUntil)
            .IsRequired(false);

        // Relationship with User
        builder.HasOne(x => x.User)
            .WithOne(u => u.Mfa)
            .HasForeignKey<UserMfa>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasQueryFilter(um => um.User != null && !um.User.IsDeleted);

        // Unique index on UserId (one MFA record per user)
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("UX_UserMfa_UserId");

        // Indexes for monitoring and fast access
        builder.HasIndex(x => new { x.UserId, x.IsEnabled, x.IsLocked })
            .HasDatabaseName("IX_UserMfa_User_Enabled_Locked");

        builder.HasIndex(x => x.LastVerifiedAt)
            .HasDatabaseName("IX_UserMfa_LastVerifiedAt")
            .HasFilter("[LastVerifiedAt] IS NOT NULL");

        builder.HasIndex(x => x.IsEnabled)
            .HasDatabaseName("IX_UserMfa_IsEnabled");

        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("IX_UserMfa_IsLocked");

        builder.HasIndex(x => x.LockedUntil)
            .HasDatabaseName("IX_UserMfa_LockedUntil")
            .HasFilter("[LockedUntil] IS NOT NULL");

        // Check constraints
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_UserMfa_LockRange",
                "([LockedUntil] IS NULL AND [LockedAt] IS NULL) OR ([LockedUntil] >= [LockedAt])");

            tb.HasCheckConstraint(
                "CK_UserMfa_Attempts_NonNegative",
                "[Attempts] >= 0");
        });
    }
}