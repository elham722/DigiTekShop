namespace DigiTekShop.Identity.Configurations;
    internal class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.Property(r => r.CreatedAt)
                .IsRequired().HasDefaultValueSql("GETUTCDATE()");

            builder.Property(r => r.UpdatedAt)
                .IsRequired(false).HasDefaultValueSql("GETUTCDATE()");

            // Configure relationships
            builder.HasMany(r => r.Permissions)
                .WithOne(rp => rp.Role)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            builder.HasIndex(r => r.CreatedAt)
                .HasDatabaseName("IX_Roles_CreatedAt");
        }
    }