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
                .HasMaxLength(512);

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

            builder.HasIndex(rt => rt.ExpiresAt)
                .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

            builder.HasIndex(rt => rt.IsRevoked)
                .HasDatabaseName("IX_RefreshTokens_IsRevoked");

            builder.HasIndex(rt => rt.CreatedAt)
                .HasDatabaseName("IX_RefreshTokens_CreatedAt");
        }
    }