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
                .HasMaxLength(128); // Base64 SHA-512 = 88 chars, با حاشیه امنیت

            builder.Property(rt => rt.ExpiresAt)
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(rt => rt.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rt => rt.RevokedAt)
                .IsRequired(false);

            builder.Property(rt => rt.RevokedReason)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(rt => rt.DeviceId)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(rt => rt.UserAgent)
                .HasMaxLength(512)
                .IsRequired(false);

            builder.Property(rt => rt.CreatedByIp)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired(false);

            builder.Property(rt => rt.ParentTokenHash)
                .HasMaxLength(128)
                .IsRequired(false);

            builder.Property(rt => rt.ReplacedByTokenHash)
                .HasMaxLength(128)
                .IsRequired(false);

            builder.Property(rt => rt.UserId)
                .IsRequired();

            // Configure relationships
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            builder.HasIndex(rt => rt.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");

            // ایندکس مرکب برای کوئری‌های فعال
            builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt })
                .HasDatabaseName("IX_RefreshTokens_User_Active");

            // ایندکس برای مرتب‌سازی بر اساس انقضا
            builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt })
                .HasDatabaseName("IX_RefreshTokens_User_Expires");

            // ایندکس برای دستگاه‌ها
            builder.HasIndex(rt => new { rt.UserId, rt.DeviceId })
                .HasDatabaseName("IX_RefreshTokens_User_Device");

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

    }
}