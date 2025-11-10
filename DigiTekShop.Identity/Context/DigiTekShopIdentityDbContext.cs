namespace DigiTekShop.Identity.Context;

public class DigiTekShopIdentityDbContext : IdentityDbContext<User, Role, Guid>
{
    public DigiTekShopIdentityDbContext(DbContextOptions<DigiTekShopIdentityDbContext> options) : base(options)
    {
    }


    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();
    public DbSet<UserMfa> UserMfa => Set<UserMfa>();
    public DbSet<IdentityOutboxMessage> IdentityOutboxMessages => Set<IdentityOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(DigiTekShopIdentityDbContext).Assembly);

        // Map Identity tables (Role table name is set in RoleConfiguration)
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}
