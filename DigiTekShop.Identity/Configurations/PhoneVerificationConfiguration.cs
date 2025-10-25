namespace DigiTekShop.Identity.Configurations;

public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.CodeHash).IsRequired().HasMaxLength(256);
        builder.Property(pv => pv.CodeHashAlgo).HasMaxLength(32);
        builder.Property(pv => pv.SecretVersion).HasDefaultValue(1);

        builder.Property(pv => pv.EncryptedCodeProtected)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(pv => pv.DeviceId).HasMaxLength(128);

        builder.Property(pv => pv.Attempts).HasDefaultValue(0);
        builder.Property(pv => pv.CreatedAtUtc).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(pv => pv.ExpiresAtUtc).IsRequired();
        builder.Property(pv => pv.IsVerified).HasDefaultValue(false);

        builder.Property(pv => pv.PhoneNumber).HasMaxLength(32);
        builder.Property(pv => pv.IpAddress).HasMaxLength(45);
        builder.Property(pv => pv.UserAgent).HasMaxLength(512);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasIndex(pv => pv.PhoneNumber).HasDatabaseName("IX_PhoneVerifications_Phone");
        builder.HasIndex(pv => pv.ExpiresAtUtc).HasDatabaseName("IX_PhoneVerifications_ExpiresAt");
        builder.HasIndex(pv => pv.CreatedAtUtc).HasDatabaseName("IX_PhoneVerifications_CreatedAt");

        builder.HasIndex(pv => new { pv.PhoneNumber, pv.IsVerified, pv.ExpiresAtUtc })
            .HasDatabaseName("IX_PV_Phone_Active");

        builder.HasIndex(pv => new { pv.UserId, pv.IsVerified, pv.ExpiresAtUtc })
            .HasDatabaseName("IX_PV_User_Active");

        builder.HasIndex(pv => new { pv.PhoneNumber, pv.CodeHash, pv.ExpiresAtUtc })
            .IsUnique()
            .HasDatabaseName("UX_PhoneVerifications_Phone_Code_ExpiresAt");
    }
}

