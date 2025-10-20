namespace DigiTekShop.Identity.Configurations;
    internal class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);
           
            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(128).IsUnicode(false); 

            builder.Property(rt => rt.ExpiresAtUtc)
                .HasColumnType("datetime2(3)")
                .IsRequired();

            builder.Property(rt => rt.RowVersion)
                .IsRowVersion()
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(rt => rt.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rt => rt.RevokedAtUtc)
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
                .HasMaxLength(45) 
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.ParentTokenHash)
                .HasMaxLength(128)
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.ReplacedByTokenHash)
                .HasMaxLength(128)
                .IsRequired(false).IsUnicode(false);

            builder.Property(rt => rt.UserId)
                .IsRequired();

            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

        builder.HasIndex(rt => rt.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => new { rt.UserId, rt.DeviceId, rt.IsRevoked, rt.ExpiresAtUtc })
            .HasDatabaseName("IX_RefreshTokens_User_Device_Active")
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.CreatedAtUtc });

        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAtUtc })
            .HasDatabaseName("IX_RefreshTokens_User_Active")
            .IncludeProperties(rt => new { rt.Id, rt.TokenHash, rt.DeviceId, rt.CreatedAtUtc });

        builder.HasIndex(rt => rt.ExpiresAtUtc)
                .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

            builder.HasIndex(rt => rt.IsRevoked)
                .HasDatabaseName("IX_RefreshTokens_IsRevoked");

            builder.HasIndex(rt => rt.CreatedAtUtc)
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