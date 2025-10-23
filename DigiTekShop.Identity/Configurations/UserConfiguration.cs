using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigiTekShop.Identity.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.CustomerId).IsRequired(false);

        b.Property(u => u.NormalizedPhoneNumber)
            .HasMaxLength(64)
            .IsRequired(false);

        b.Property(u => u.IsDeleted).HasDefaultValue(false);
        b.Property(u => u.CreatedAtUtc).IsRequired().HasDefaultValueSql("GETUTCDATE()");
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


      
        b.HasIndex(u => u.NormalizedEmail)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_NormalizedEmail_Active");

        b.HasIndex(u => u.Email)
            .HasDatabaseName("IX_Users_Email_Active");

        b.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Users_NormalizedUserName_Active");

        b.HasIndex(u => u.PhoneNumber)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_Phone_Active");

        b.HasIndex(u => u.NormalizedPhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [NormalizedPhoneNumber] IS NOT NULL")
            .HasDatabaseName("UX_Users_NormalizedPhone_Active");


    }
}
