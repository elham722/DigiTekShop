namespace DigiTekShop.Identity.Configurations;
    internal class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
    {
        public void Configure(EntityTypeBuilder<PhoneVerification> builder)
        {
            // Configure primary key
            builder.HasKey(pv => pv.Id);

            // Configure properties
            builder.Property(pv => pv.UserId)
                .IsRequired();

            builder.Property(pv => pv.CodeHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(pv => pv.ExpiresAt)
                .IsRequired();

            builder.Property(pv => pv.Attempts)
                .HasDefaultValue(0);

            builder.Property(pv => pv.CreatedAt)
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(pv => pv.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(pv => pv.IsVerified)
                .HasDefaultValue(false);

            builder.Property(pv => pv.VerifiedAt)
                .IsRequired(false);

            builder.Property(pv => pv.IpAddress)
                .HasMaxLength(45) // IPv6 max length
                .IsRequired(false);

            builder.Property(pv => pv.UserAgent)
                .HasMaxLength(512)
                .IsRequired(false);

            // Configure relationships
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(pv => pv.UserId)
                .HasDatabaseName("IX_PhoneVerifications_UserId");

            builder.HasIndex(pv => pv.ExpiresAt)
                .HasDatabaseName("IX_PhoneVerifications_ExpiresAt");

            builder.HasIndex(pv => pv.CreatedAt)
                .HasDatabaseName("IX_PhoneVerifications_CreatedAt");

            builder.HasIndex(pv => new { pv.UserId, pv.ExpiresAt })
                .HasDatabaseName("IX_PhoneVerifications_UserId_ExpiresAt");

            builder.HasIndex(pv => new { pv.UserId, pv.CreatedAt })
                .HasDatabaseName("IX_PhoneVerifications_UserId_CreatedAt");

            builder.HasIndex(pv => new { pv.UserId, pv.CodeHash, pv.ExpiresAt })
                .IsUnique()
                .HasDatabaseName("UX_PhoneVerifications_User_Code_ExpiresAt");

          
            builder.HasIndex(pv => new { pv.UserId, pv.IsVerified })
                .HasDatabaseName("IX_PhoneVerifications_User_IsVerified");

    }
}
