

namespace DigiTekShop.Identity.Context;
    public class DigiTekShopIdentityDbContext:IdentityDbContext<User,Role,string>
    {
        public DigiTekShopIdentityDbContext(DbContextOptions<DigiTekShopIdentityDbContext> options):base(options)
        {
            
        }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
}
