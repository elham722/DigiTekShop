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
            .HasMaxLength(256)
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45) // IPv6
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.ParentTokenHash)
            .HasMaxLength(128)
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(128)
            .IsRequired(false)
            .IsUnicode(false);

        builder.Property(rt => rt.UsageCount)
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

        builder.HasIndex(rt => rt.ParentTokenHash)
            .HasDatabaseName("IX_RefreshTokens_ParentTokenHash");

        builder.HasIndex(rt => rt.ReplacedByTokenHash)
            .HasDatabaseName("IX_RefreshTokens_ReplacedByTokenHash");


        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAfterCreated",
                "[ExpiresAtUtc] > [CreatedAtUtc]");

            tb.HasCheckConstraint(
                "CK_RefreshTokens_RotationConsistency",
                "(([RotatedAtUtc] IS NULL AND [ReplacedByTokenHash] IS NULL) OR ([RotatedAtUtc] IS NOT NULL AND [ReplacedByTokenHash] IS NOT NULL))");

            tb.HasCheckConstraint(
                "CK_RefreshTokens_RevokedConsistency",
                "(([RevokedAtUtc] IS NULL AND [RevokedReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL))");
        });
    }
}
