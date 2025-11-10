using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        // Composite primary key ensures uniqueness of (UserId, PermissionId)
        // Consistent with RolePermission pattern
        builder.HasKey(up => new { up.UserId, up.PermissionId });

        builder.Property(up => up.IsGranted)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(up => up.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(up => up.UpdatedAt)
            .IsRequired(false);

        builder.Property(up => up.RowVersion)
            .IsRowVersion()
            .IsRequired(false);

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasQueryFilter(up => up.User != null && !up.User.IsDeleted);

        // Composite index for fast listing of granted permissions per user
        builder.HasIndex(up => new { up.UserId, up.IsGranted })
            .HasDatabaseName("IX_UserPermissions_User_Granted");

        // Index for reverse queries: "all users for a specific permission"
        // Since PK starts with UserId, queries filtering by PermissionId benefit from this index
        builder.HasIndex(up => up.PermissionId)
            .HasDatabaseName("IX_UserPermissions_PermissionId");

        // Index for time-based queries and sorting
        builder.HasIndex(up => up.CreatedAt)
            .HasDatabaseName("IX_UserPermissions_CreatedAt");

        // Check constraints
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_UserPermissions_Updated_GTE_Created",
                "([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt])");
        });

        // Note: No need for unique index on (UserId, PermissionId) - PK already enforces uniqueness
    }
}