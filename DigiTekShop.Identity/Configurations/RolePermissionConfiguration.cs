using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations
{
    internal class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            // Configure composite primary key
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure properties
            builder.Property(rp => rp.CreatedAt)
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Configure relationships
            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.Roles)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(rp => rp.CreatedAt)
                .HasDatabaseName("IX_RolePermissions_CreatedAt");
        }
    }
}