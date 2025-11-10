using DigiTekShop.SharedKernel.Enums.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    // Field length constants
    private const int MaxPhoneNumberLength = 32;
    private const int MaxPhoneNumberNormalizedLength = 20; // E.164 max length
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 1024;
    private const int MaxDeviceIdLength = 128;
    private const int MaxCodeHashLength = 256;
    private const int MaxCodeHashAlgoLength = 32;

    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("PhoneVerifications");
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.CodeHash)
            .IsRequired()
            .HasMaxLength(MaxCodeHashLength);

        builder.Property(pv => pv.CodeHashAlgo)
            .HasMaxLength(MaxCodeHashAlgoLength)
            .IsRequired(false);

        builder.Property(pv => pv.SecretVersion)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(pv => pv.EncryptedCodeProtected)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(pv => pv.DeviceId)
            .HasMaxLength(MaxDeviceIdLength)
            .IsRequired(false);

        builder.Property(pv => pv.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(pv => pv.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(pv => pv.ExpiresAtUtc)
            .IsRequired();

        builder.Property(pv => pv.VerifiedAtUtc)
            .IsRequired(false);

        builder.Property(pv => pv.LockedUntilUtc)
            .IsRequired(false);

        builder.Property(pv => pv.IsVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(pv => pv.PhoneNumber)
            .HasMaxLength(MaxPhoneNumberLength)
            .IsRequired(false);

        builder.Property(pv => pv.PhoneNumberNormalized)
            .HasMaxLength(MaxPhoneNumberNormalizedLength)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(pv => pv.IpAddress)
            .HasMaxLength(MaxIpAddressLength)
            .IsRequired(false);

        builder.Property(pv => pv.UserAgent)
            .HasMaxLength(MaxUserAgentLength)
            .IsRequired(false);

        builder.Property(pv => pv.Purpose)
            .HasConversion<byte>()
            .HasDefaultValue((byte)VerificationPurpose.Login)
            .IsRequired();

        builder.Property(pv => pv.Channel)
            .HasConversion<byte>()
            .HasDefaultValue((byte)VerificationChannel.Sms)
            .IsRequired();

        builder.Property(pv => pv.RowVersion)
            .IsRowVersion();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.SetNull) // Preserve security audit trail when user is deleted
            .IsRequired(false);

        // Indexes
        builder.HasIndex(pv => pv.PhoneNumber)
            .HasDatabaseName("IX_PhoneVerifications_Phone");

        builder.HasIndex(pv => pv.PhoneNumberNormalized)
            .HasDatabaseName("IX_PhoneVerifications_PhoneNormalized")
            .HasFilter("[PhoneNumberNormalized] IS NOT NULL");

        builder.HasIndex(pv => pv.ExpiresAtUtc)
            .HasDatabaseName("IX_PhoneVerifications_ExpiresAt");

        builder.HasIndex(pv => pv.CreatedAtUtc)
            .HasDatabaseName("IX_PhoneVerifications_CreatedAt");

        builder.HasIndex(pv => pv.LockedUntilUtc)
            .HasDatabaseName("IX_PhoneVerifications_LockedUntil")
            .HasFilter("[LockedUntilUtc] IS NOT NULL");

        builder.HasIndex(pv => new { pv.PhoneNumberNormalized, pv.IsVerified, pv.ExpiresAtUtc })
            .HasDatabaseName("IX_PV_PhoneNormalized_Active");

        builder.HasIndex(pv => new { pv.UserId, pv.IsVerified, pv.ExpiresAtUtc })
            .HasDatabaseName("IX_PV_User_Active");

        builder.HasIndex(pv => new { pv.Purpose, pv.Channel, pv.ExpiresAtUtc })
            .HasDatabaseName("IX_PV_Purpose_Channel_ExpiresAt");

        // Unique filtered index: Only one unverified OTP per phone/purpose/channel combination
        // Note: Cannot use non-deterministic functions (SYSUTCDATETIME) in index filter
        // The "active" logic (not expired) is enforced in application layer
        builder.HasIndex(pv => new { pv.PhoneNumberNormalized, pv.Purpose, pv.Channel })
            .IsUnique()
            .HasFilter("[PhoneNumberNormalized] IS NOT NULL AND [IsVerified] = 0")
            .HasDatabaseName("UX_PV_Active_Phone_Purpose_Channel");
    }
}

