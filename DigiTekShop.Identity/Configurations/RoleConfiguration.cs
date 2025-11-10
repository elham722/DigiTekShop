using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Field length constants
    private const int MaxNameLength = 256;

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Explicitly set table name
        builder.ToTable("Roles");

        // Configure base Identity properties
        builder.Property(r => r.Name)
            .HasMaxLength(MaxNameLength);

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(MaxNameLength);

        // Configure custom properties
        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        // Configure relationships
        builder.HasMany(r => r.Permissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        // Critical: Unique index on NormalizedName for case-insensitive role name uniqueness
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("RoleNameIndex")
            .HasFilter("[NormalizedName] IS NOT NULL");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Roles_CreatedAt");
    }
}