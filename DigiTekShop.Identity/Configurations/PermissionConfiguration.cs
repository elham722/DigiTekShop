namespace DigiTekShop.Identity.Configurations;
    internal class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            // Configure primary key
            builder.HasKey(p => p.Id);

            // Configure properties
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.Property(p => p.CreatedAt)
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);

            // Configure relationships
            builder.HasMany(p => p.Roles)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("IX_Permissions_Name");

            builder.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Permissions_IsActive");

            builder.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Permissions_CreatedAt");
        }
    }