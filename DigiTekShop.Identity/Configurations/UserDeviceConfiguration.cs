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
                .HasMaxLength(256);

            builder.Property(ud => ud.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            builder.Property(ud => ud.LastLoginAt)
                .IsRequired();

            builder.Property(ud => ud.IsActive)
                .HasDefaultValue(true);

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
        }
    }