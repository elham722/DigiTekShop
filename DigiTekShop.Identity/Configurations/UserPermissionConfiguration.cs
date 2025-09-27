using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations
{
    internal class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            // Configure primary key
            builder.HasKey(up => up.Id);

            // Configure properties
            builder.Property(up => up.UserId)
                .IsRequired();

            builder.Property(up => up.PermissionId)
                .IsRequired();

            builder.Property(up => up.IsGranted)
                .HasDefaultValue(true);

            // Configure relationships
            builder.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(up => up.UserId)
                .HasDatabaseName("IX_UserPermissions_UserId");

            builder.HasIndex(up => up.PermissionId)
                .HasDatabaseName("IX_UserPermissions_PermissionId");

            builder.HasIndex(up => new { up.UserId, up.PermissionId })
                .IsUnique()
                .HasDatabaseName("IX_UserPermissions_UserId_PermissionId");

            builder.HasIndex(up => up.IsGranted)
                .HasDatabaseName("IX_UserPermissions_IsGranted");
        }
    }
}