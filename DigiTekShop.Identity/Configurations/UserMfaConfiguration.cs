using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations
{
    internal class UserMfaConfiguration : IEntityTypeConfiguration<UserMfa>
    {
        public void Configure(EntityTypeBuilder<UserMfa> builder)
        {
            builder.ToTable("UserMfa");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.SecretKeyEncrypted)
                .IsRequired();  

            builder.Property(x => x.IsEnabled)
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.LastVerifiedAt)
                .IsRequired(false);

            builder.Property(x => x.IsLocked)
                .HasDefaultValue(false);

            builder.Property(x => x.LockedAt)
                .IsRequired(false);

            builder.Property(x => x.LockedUntil)
                .IsRequired(false);

            builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("UX_UserMfa_UserId");
            builder.HasIndex(x => x.IsEnabled).HasDatabaseName("IX_UserMfa_IsEnabled");
            builder.HasIndex(x => x.IsLocked).HasDatabaseName("IX_UserMfa_IsLocked");
            builder.HasIndex(x => x.LockedUntil).HasDatabaseName("IX_UserMfa_LockedUntil");

        }
    }
}