using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.CustomerId).IsRequired(false);

        b.Property(u => u.NormalizedPhoneNumber)
            .HasMaxLength(64)
            .IsRequired(false);

        b.Property(u => u.IsDeleted).HasDefaultValue(false).IsRequired();
        b.Property(u => u.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();
        b.Property(u => u.UpdatedAtUtc).IsRequired(false);
        b.Property(u => u.DeletedAtUtc).IsRequired(false);
        b.Property(u => u.LastLoginAtUtc).IsRequired(false);

        b.HasMany(u => u.Devices)
            .WithOne(ud => ud.User)
            .HasForeignKey(ud => ud.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(u => u.UserPermissions)
            .WithOne(up => up.User)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        
        b.HasQueryFilter(u => !u.IsDeleted);


        // Override Identity's default indexes with filtered versions to support soft-delete
        // Using Identity's default index names ensures EF replaces them instead of creating duplicates

        // Override Identity's default "UserNameIndex" (unique on NormalizedUserName)
        b.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UserNameIndex");

        // Override Identity's default "EmailIndex" (non-unique on NormalizedEmail)
        b.HasIndex(u => u.NormalizedEmail)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("EmailIndex");

        // Additional index on non-normalized Email for queries that don't use normalized values
        b.HasIndex(u => u.Email)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_Email_Active");

        b.HasIndex(u => u.PhoneNumber)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_Phone_Active");

        b.HasIndex(u => u.NormalizedPhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [NormalizedPhoneNumber] IS NOT NULL")
            .HasDatabaseName("UX_Users_NormalizedPhone_Active");


    }
}
