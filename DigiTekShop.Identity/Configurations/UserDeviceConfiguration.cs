namespace DigiTekShop.Identity.Configurations;
    internal class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
    {
        public void Configure(EntityTypeBuilder<UserDevice> builder)
        {
            // Configure primary key
            builder.HasKey(ud => ud.Id);

            // Configure properties
            builder.Property(ud => ud.DeviceName)
                .IsRequired()
                .HasMaxLength(64); 

            builder.Property(ud => ud.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            builder.Property(ud => ud.DeviceFingerprint)
                .HasMaxLength(256)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(ud => ud.BrowserInfo)
                .HasMaxLength(128)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(ud => ud.OperatingSystem)
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(ud => ud.LastLoginAt)
                .HasColumnType("datetime2(3)")
                .IsRequired();

            builder.Property(ud => ud.IsActive)
                .HasDefaultValue(true);

            builder.Property(ud => ud.IsTrusted)
                .HasDefaultValue(false);

            builder.Property(ud => ud.TrustedAt)
                .IsRequired(false);

            builder.Property(ud => ud.TrustExpiresAt)
                .IsRequired(false);

            builder.Property(ud => ud.UserId)
                .IsRequired();

            // Configure relationships
            builder.HasOne(ud => ud.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(ud => ud.UserId)
                .HasDatabaseName("IX_UserDevices_UserId");

            builder.HasIndex(ud => ud.IpAddress)
                .HasDatabaseName("IX_UserDevices_IpAddress");

            builder.HasIndex(ud => ud.IsActive)
                .HasDatabaseName("IX_UserDevices_IsActive");

            builder.HasIndex(ud => ud.LastLoginAt)
                .HasDatabaseName("IX_UserDevices_LastLoginAt");

            builder.HasIndex(ud => new { ud.UserId, ud.DeviceFingerprint })
                .IsUnique()
                .HasDatabaseName("UX_UserDevices_User_DeviceFingerprint")
                .HasFilter("[DeviceFingerprint] IS NOT NULL");

            builder.HasIndex(ud => new { ud.UserId, ud.IsActive })
                .HasDatabaseName("IX_UserDevices_User_IsActive");

            builder.HasIndex(ud => new { ud.UserId, ud.IsTrusted })
                .HasDatabaseName("IX_UserDevices_User_IsTrusted");

    }
}