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

            builder.HasIndex(pv => new { pv.UserId, pv.CodeHash, pv.ExpiresAt })
                .IsUnique()
                .HasDatabaseName("UX_PhoneVerifications_User_Code_ExpiresAt");

    }
}
