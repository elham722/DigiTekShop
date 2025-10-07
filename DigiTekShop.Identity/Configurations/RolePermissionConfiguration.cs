namespace DigiTekShop.Identity.Configurations;
    internal class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

           
            builder.Property(rp => rp.CreatedAt)
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.Roles)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rp => rp.CreatedAt)
                .HasDatabaseName("IX_RolePermissions_CreatedAt");

           
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique()
                .HasDatabaseName("UX_RolePermissions_Role_Permission");
        }
    }