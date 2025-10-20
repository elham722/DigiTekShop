namespace DigiTekShop.Identity.Configurations;
internal class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> b)
    {
        b.ToTable("UserDevices");
        b.HasKey(x => x.Id);

        b.Property(x => x.UserId).IsRequired();

        b.Property(x => x.DeviceId)
            .IsRequired()
            .HasMaxLength(64)        
            .IsUnicode(false);

        b.Property(x => x.DeviceName)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.DeviceFingerprint)
            .HasMaxLength(256)
            .IsUnicode(false);

        b.Property(x => x.BrowserInfo)
            .HasMaxLength(512)       
            .IsUnicode(false);

        b.Property(x => x.OperatingSystem)
            .HasMaxLength(64);

        b.Property(x => x.LastIp)
            .HasMaxLength(45)        
            .IsUnicode(false);

        b.Property(x => x.FirstSeenUtc).HasColumnType("datetime2(3)").IsRequired();
        b.Property(x => x.LastSeenUtc).HasColumnType("datetime2(3)").IsRequired();

        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.Property(x => x.TrustedAtUtc);
        b.Property(x => x.TrustedUntilUtc);
        b.Property(x => x.TrustCount).HasDefaultValue(0);

        b.HasOne(x => x.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        
        b.HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique()
            .HasDatabaseName("UX_UserDevices_User_DeviceId");

        b.HasIndex(x => x.UserId).HasDatabaseName("IX_UserDevices_UserId");
        b.HasIndex(x => x.LastSeenUtc).HasDatabaseName("IX_UserDevices_LastSeenUtc");
        b.HasIndex(x => new { x.UserId, x.TrustedUntilUtc }).HasDatabaseName("IX_UserDevices_User_TrustedUntil");

        b.HasIndex(x => new { x.UserId, x.DeviceFingerprint })
            .IsUnique()
            .HasFilter("[DeviceFingerprint] IS NOT NULL")
            .HasDatabaseName("UX_UserDevices_User_Fingerprint");

        b.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("IX_UserDevices_User_IsActive");
    }
}
