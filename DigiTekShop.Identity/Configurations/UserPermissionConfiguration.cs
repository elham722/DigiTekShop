using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Identity.Configurations;

internal class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

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

       
        builder.HasIndex(up => new { up.UserId, up.IsGranted })
            .HasDatabaseName("IX_UserPermissions_User_Granted");

       
        builder.HasIndex(up => up.PermissionId)
            .HasDatabaseName("IX_UserPermissions_PermissionId");

        builder.HasIndex(up => up.CreatedAt)
            .HasDatabaseName("IX_UserPermissions_CreatedAt");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint(
                "CK_UserPermissions_Updated_GTE_Created",
                "([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt])");
        });

    }
}