

namespace DigiTekShop.Identity.Context;
    public class DigiTekShopIdentityDbContext:IdentityDbContext<User,Role,Guid>
    {
        public DigiTekShopIdentityDbContext(DbContextOptions<DigiTekShopIdentityDbContext> options):base(options)
        {
            
        }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
        public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();
}
