namespace DigiTekShop.Identity.Configurations;
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.CustomerId)
                .IsRequired(false);

            builder.Property(u => u.GoogleId)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(u => u.MicrosoftId)
                .HasMaxLength(256)
                .IsRequired(false);


            builder.Property(u => u.IsDeleted)
                .HasDefaultValue(false);
            builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            builder.Property(u => u.UpdatedAt).IsRequired(false);

            builder.Property(u => u.DeletedAt)
                .IsRequired(false);

            builder.Property(u => u.LastPasswordChangeAt)
                .IsRequired(false);

            builder.Property(u => u.LastLoginAt)
                .IsRequired(false);

            // Configure relationships
            builder.HasMany(u => u.Devices)
                .WithOne(ud => ud.User)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.UserPermissions)
                .WithOne(up => up.User)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.PasswordResetTokens)
                .WithOne(prt => prt.User)
                .HasForeignKey(prt => prt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Mfa)
            .WithOne(um => um.User)            
            .HasForeignKey<UserMfa>(um => um.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(u => !u.IsDeleted);

        // Configure indexes
        builder.HasIndex(u => u.CustomerId)
                .HasDatabaseName("IX_Users_CustomerId");

            builder.HasIndex(u => u.GoogleId)
                .HasDatabaseName("IX_Users_GoogleId");

            builder.HasIndex(u => u.MicrosoftId)
                .HasDatabaseName("IX_Users_MicrosoftId");

            builder.HasIndex(u => u.IsDeleted)
                .HasDatabaseName("IX_Users_IsDeleted");

            builder.HasIndex(u => u.NormalizedEmail)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_Users_NormalizedEmail_Active");

            builder.HasIndex(u => u.NormalizedUserName)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_Users_NormalizedUserName_Active");

    }

}
