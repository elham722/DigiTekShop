namespace DigiTekShop.Identity.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(rt => rt.ExpiresAtUtc)
            .IsRequired();

        builder.Property(rt => rt.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()"); 

        builder.Property(rt => rt.LastUsedAtUtc)
            .IsRequired(false);

        builder.Property(rt => rt.RevokedAtUtc)
            .IsRequired(false);

        builder.Property(rt => rt.RotatedAtUtc)
            .IsRequired(false);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(rt => rt.DeviceId)
            .HasMaxLength(128) // Consistent with other models (LoginAttempt, PhoneVerification)
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(1024) // Consistent with other models (AuditLog, SecurityEvent)
            .IsRequired(false);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45) // IPv6
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.ParentTokenId)
            .IsRequired(false);

        builder.Property(rt => rt.ReplacedByTokenId)
            .IsRequired(false);

        builder.Property(rt => rt.UsageCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(rt => rt.RowVersion)
            .IsRowVersion()
            .IsRequired();

     


        builder.Ignore(rt => rt.IsRevoked);
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsActive);

        
        builder.Property(rt => rt.UserId).IsRequired();
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasQueryFilter(rt => rt.User != null && !rt.User.IsDeleted);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(rt => new { rt.UserId, rt.DeviceId, rt.ExpiresAtUtc, rt.RevokedAtUtc })
            .HasDatabaseName("IX_RefreshTokens_User_Device_Active")
            .HasFilter("[RevokedAtUtc] IS NULL") 
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.CreatedAtUtc });

        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAtUtc, rt.RevokedAtUtc })
            .HasDatabaseName("IX_RefreshTokens_User_Active")
            .HasFilter("[RevokedAtUtc] IS NULL")
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.DeviceId, rt.CreatedAtUtc });

        builder.HasIndex(rt => rt.ExpiresAtUtc)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAtUtc");

        builder.HasIndex(rt => rt.RevokedAtUtc)
            .HasDatabaseName("IX_RefreshTokens_RevokedAtUtc");

        builder.HasIndex(rt => rt.CreatedAtUtc)
            .HasDatabaseName("IX_RefreshTokens_CreatedAtUtc");

        // Foreign keys for token rotation chain (self-referencing)
        builder.HasOne(rt => rt.ParentToken)
            .WithMany()
            .HasForeignKey(rt => rt.ParentTokenId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(rt => rt.ReplacedByToken)
            .WithMany()
            .HasForeignKey(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(rt => rt.ParentTokenId)
            .HasDatabaseName("IX_RefreshTokens_ParentTokenId")
            .HasFilter("[ParentTokenId] IS NOT NULL");

        builder.HasIndex(rt => rt.ReplacedByTokenId)
            .HasDatabaseName("IX_RefreshTokens_ReplacedByTokenId")
            .HasFilter("[ReplacedByTokenId] IS NOT NULL");

        // Optional: Unique index to enforce "one active token per user per device"
        // Note: Expiration check must be enforced in application layer (cannot use non-deterministic functions in filter)
        builder.HasIndex(rt => new { rt.UserId, rt.DeviceId })
            .IsUnique()
            .HasFilter("[RevokedAtUtc] IS NULL AND [DeviceId] IS NOT NULL")
            .HasDatabaseName("UX_RefreshTokens_User_Device_Active");


        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAfterCreated",
                "[ExpiresAtUtc] > [CreatedAtUtc]");

            tb.HasCheckConstraint(
                "CK_RefreshTokens_RotationConsistency",
                "(([RotatedAtUtc] IS NULL AND [ReplacedByTokenId] IS NULL) OR ([RotatedAtUtc] IS NOT NULL AND [ReplacedByTokenId] IS NOT NULL))");

            tb.HasCheckConstraint(
                "CK_RefreshTokens_RevokedConsistency",
                "(([RevokedAtUtc] IS NULL AND [RevokedReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL))");

            // LastUsedAtUtc must be between CreatedAtUtc and ExpiresAtUtc
            tb.HasCheckConstraint(
                "CK_RefreshTokens_LastUsedRange",
                "([LastUsedAtUtc] IS NULL OR ([LastUsedAtUtc] >= [CreatedAtUtc] AND [LastUsedAtUtc] <= [ExpiresAtUtc]))");

            // RevokedAtUtc must be after CreatedAtUtc
            tb.HasCheckConstraint(
                "CK_RefreshTokens_RevokedAfterCreated",
                "([RevokedAtUtc] IS NULL OR [RevokedAtUtc] >= [CreatedAtUtc])");
        });
    }
}
