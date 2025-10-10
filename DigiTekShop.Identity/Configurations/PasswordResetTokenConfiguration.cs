using DigiTekShop.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations
{
    internal class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(256); 

            builder.Property(x => x.IpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.AttemptCount)
                .HasDefaultValue(0);

            builder.Property(x => x.LastAttemptAt)
                .IsRequired(false);

            builder.Property(x => x.ThrottleUntil)
                .IsRequired(false);

            builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
            builder.HasIndex(x => x.TokenHash).IsUnique();
            
            builder.HasIndex(x => new { x.UserId, x.IsUsed, x.ExpiresAt })
                   .HasDatabaseName("IX_PasswordResetTokens_User_Active");
            
            builder.HasIndex(x => new { x.UserId, x.ThrottleUntil })
                   .HasDatabaseName("IX_PasswordResetTokens_User_Throttle");

            builder.HasOne(x => x.User)
                .WithMany(u => u.PasswordResetTokens) 
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

        }
    }
}