using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    // Field length constants
    private const int MaxNameLength = 256;
    private const int MaxDescriptionLength = 1000;

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        // Configure primary key
        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(MaxNameLength);

        builder.Property(p => p.Description)
            .HasMaxLength(MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        // Configure relationships
        builder.HasMany(p => p.Roles)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.UserPermissions)
            .WithOne(up => up.Permission)
            .HasForeignKey(up => up.PermissionId)
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