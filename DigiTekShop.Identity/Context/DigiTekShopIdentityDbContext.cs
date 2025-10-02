namespace DigiTekShop.Identity.Context;

public class DigiTekShopIdentityDbContext : IdentityDbContext<User, Role, Guid>
{
    public DigiTekShopIdentityDbContext(DbContextOptions<DigiTekShopIdentityDbContext> options) : base(options)
    {
    }

    // DbSets for custom entities
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<UserMfa> UserMfa => Set<UserMfa>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new PermissionConfiguration());
        builder.ApplyConfiguration(new RolePermissionConfiguration());
        builder.ApplyConfiguration(new UserPermissionConfiguration());
        builder.ApplyConfiguration(new UserDeviceConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new AuditLogConfiguration());
        builder.ApplyConfiguration(new LoginAttemptConfiguration());
        builder.ApplyConfiguration(new PhoneVerificationConfiguration());
        builder.ApplyConfiguration(new PasswordHistoryConfiguration());
        builder.ApplyConfiguration(new UserMfaConfiguration());

        // Configure Identity table names (optional - for custom naming)
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}
