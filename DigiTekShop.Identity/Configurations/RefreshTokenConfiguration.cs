namespace DigiTekShop.Identity.Configurations;
    internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Configure primary key
            builder.HasKey(rt => rt.Id);

            // Configure properties
            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(128).IsUnicode(false); // HMACSHA256 Base64Url (no padding) ≈ 43 chars, with safety margin

            builder.Property(rt => rt.ExpiresAt)
                .HasColumnType("datetime2(3)")
                .IsRequired();

            // ✅ Optimistic Concurrency Control: RowVersion (timestamp in SQL Server)
            builder.Property(rt => rt.RowVersion)
                .IsRowVersion()
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(rt => rt.CreatedAt)
                .HasColumnType("datetime2(3)")
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rt => rt.RevokedAt)
                .HasColumnType("datetime2(3)")
                .IsRequired(false);

            builder.Property(rt => rt.RevokedReason)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(rt => rt.DeviceId)
                .HasMaxLength(256)
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.UserAgent)
                .HasMaxLength(512)
                .IsRequired(false);

            builder.Property(rt => rt.CreatedByIp)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.ParentTokenHash)
                .HasMaxLength(128)
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.ReplacedByTokenHash)
                .HasMaxLength(128)
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.UserId)
                .IsRequired();

            // Configure relationships
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            
            // ✅ Unique index on TokenHash (critical for security)
            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

        // ✅ Most important composite index for active device tokens query
        // Covers: WHERE UserId = X AND DeviceId = Y AND IsRevoked = 0 AND ExpiresAt > NOW
        // This is the most frequent query pattern in RefreshTokensAsync and RevokeActiveTokensForDeviceAsync
        builder.HasIndex(rt => new { rt.UserId, rt.DeviceId, rt.IsRevoked, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_User_Device_Active")
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.CreatedAt });

        // ✅ Composite index for user-level active tokens (without device filtering)
        // Covers: WHERE UserId = X AND IsRevoked = 0 AND ExpiresAt > NOW
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_User_Active")
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.DeviceId, rt.CreatedAt });

        // Note: Removed redundant indexes that are covered by the above composite indexes:
        // - (UserId) alone → covered by (UserId, DeviceId, IsRevoked, ExpiresAt)
        // - (UserId, ExpiresAt) → covered by (UserId, IsRevoked, ExpiresAt)
        // - (UserId, DeviceId) → covered by (UserId, DeviceId, IsRevoked, ExpiresAt)

        builder.HasIndex(rt => rt.ExpiresAt)
                .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

            builder.HasIndex(rt => rt.IsRevoked)
                .HasDatabaseName("IX_RefreshTokens_IsRevoked");

            builder.HasIndex(rt => rt.CreatedAt)
                .HasDatabaseName("IX_RefreshTokens_CreatedAt");

            builder.HasIndex(rt => rt.ParentTokenHash)
                .HasDatabaseName("IX_RefreshTokens_ParentTokenHash");

            builder.HasIndex(rt => rt.ReplacedByTokenHash)
                .HasDatabaseName("IX_RefreshTokens_ReplacedByTokenHash");
    
            builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAfterCreated",
                "[ExpiresAt] > [CreatedAt]");

            tb.HasCheckConstraint(
                "CK_RefreshTokens_RotationConsistency",
                "(([IsRotated] = 0 AND [ReplacedByTokenHash] IS NULL) OR ([IsRotated] = 1 AND [ReplacedByTokenHash] IS NOT NULL))");
        });


    }
}