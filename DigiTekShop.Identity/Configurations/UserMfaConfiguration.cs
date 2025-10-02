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

            builder.HasIndex(x => x.UserId).IsUnique();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.SecretKeyEncrypted)
                .IsRequired()
                .HasMaxLength(512);  // ✅ افزایش طول کلید رمزنگاری‌شده

            builder.Property(x => x.IsEnabled)
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.LastVerifiedAt)
                .IsRequired(false);

            builder.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserMfa>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}