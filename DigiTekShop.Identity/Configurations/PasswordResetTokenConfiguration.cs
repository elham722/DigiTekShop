using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations
{
    internal class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            // جدول
            builder.ToTable("PasswordResetTokens");

            // کلید اصلی
            builder.HasKey(x => x.Id);

            // فیلدها
            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(256); // چون SHA256 Base64 حدود 44 کاراکتر میشه

            builder.Property(x => x.IpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            // ایندکس‌ها
            builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
            builder.HasIndex(x => x.TokenHash).IsUnique();

            // رابطه با User
            builder.HasOne(x => x.User)
                .WithMany() // یا اگر خواستی تو User یه ICollection<PasswordResetToken> بزاری
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}