using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ChangedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.ChangedAt })
               .HasDatabaseName("IX_PasswordHistory_User_ChangedAt");
    }
}