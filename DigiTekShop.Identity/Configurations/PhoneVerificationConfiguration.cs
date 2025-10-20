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

            builder.Property(pv => pv.ExpiresAtUtc)
                .HasColumnType("datetime2(3)").IsRequired();

            builder.Property(pv => pv.Attempts)
                .HasDefaultValue(0);

            builder.Property(pv => pv.CreatedAtUtc)
                .IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("GETUTCDATE()");

            builder.Property(pv => pv.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(pv => pv.IsVerified)
                .HasDefaultValue(false);

            builder.Property(pv => pv.VerifiedAtUtc)
                .HasColumnType("datetime2(3)").IsRequired(false);

            builder.Property(pv => pv.IpAddress)
                .HasMaxLength(45) 
                .IsRequired(false);

            builder.Property(pv => pv.UserAgent)
                .HasMaxLength(512)
                .IsRequired(false);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();

        // Configure relationships
        builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

        // Configure indexes
        builder.HasIndex(pv => pv.UserId)
                .HasDatabaseName("IX_PhoneVerifications_UserId");

            builder.HasIndex(pv => pv.ExpiresAtUtc)
                .HasDatabaseName("IX_PhoneVerifications_ExpiresAt");

            builder.HasIndex(pv => pv.CreatedAtUtc)
                .HasDatabaseName("IX_PhoneVerifications_CreatedAt");

            builder.HasIndex(pv => new { pv.UserId, pv.ExpiresAtUtc })
                .HasDatabaseName("IX_PhoneVerifications_UserId_ExpiresAt");

            builder.HasIndex(pv => new { pv.UserId, pv.CreatedAtUtc })
                .HasDatabaseName("IX_PhoneVerifications_UserId_CreatedAt");

            builder.HasIndex(pv => new { pv.UserId, pv.CodeHash, pv.ExpiresAtUtc })
                .IsUnique()
                .HasDatabaseName("UX_PhoneVerifications_User_Code_ExpiresAt");

          
            builder.HasIndex(pv => new { pv.UserId, pv.IsVerified })
                .HasDatabaseName("IX_PhoneVerifications_User_IsVerified");

    }
}
