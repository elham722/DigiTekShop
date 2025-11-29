using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class RoleConfiguration : IEntityTypeConfiguration<Role>
{
  
    private const int MaxNameLength = 256;
    private const int MaxDescriptionLength = 1000;

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(r => r.Name)
            .HasMaxLength(MaxNameLength);

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(MaxNameLength);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        builder.Property(r => r.Description)
            .HasMaxLength(MaxDescriptionLength)
            .IsRequired(false);

        builder.Property(r => r.IsSystemRole)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.IsDefaultForNewUsers)
            .HasDefaultValue(false)
            .IsRequired();

        
        builder.HasMany(r => r.Permissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

       
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("RoleNameIndex")
            .HasFilter("[NormalizedName] IS NOT NULL");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Roles_CreatedAt");

        
        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        builder.HasIndex(r => r.IsDefaultForNewUsers)
            .HasDatabaseName("IX_Roles_IsDefaultForNewUsers");
    }
}