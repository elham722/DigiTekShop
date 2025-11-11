using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiTekShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class firstmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "User"),
                    Action = table.Column<int>(type: "int", nullable: false),
                    TargetEntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetEntityId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentityOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Type = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageKey = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    CausationId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Error = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    NextRetryUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityOutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_Outbox_Attempts_NonNegative", "[Attempts] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoginNameOrEmail = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    LoginNameOrEmailNormalized = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NormalizedPhoneNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PhoneNumberNormalized = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    Channel = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CodeHashAlgo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SecretVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EncryptedCodeProtected = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastUsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RotatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedByIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DeviceId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokens_ExpiresAfterCreated", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_RefreshTokens_LastUsedRange", "([LastUsedAtUtc] IS NULL OR ([LastUsedAtUtc] >= [CreatedAtUtc] AND [LastUsedAtUtc] <= [ExpiresAtUtc]))");
                    table.CheckConstraint("CK_RefreshTokens_RevokedAfterCreated", "([RevokedAtUtc] IS NULL OR [RevokedAtUtc] >= [CreatedAtUtc])");
                    table.CheckConstraint("CK_RefreshTokens_RevokedConsistency", "(([RevokedAtUtc] IS NULL AND [RevokedReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL))");
                    table.CheckConstraint("CK_RefreshTokens_RotationConsistency", "(([RotatedAtUtc] IS NULL AND [ReplacedByTokenId] IS NULL) OR ([RotatedAtUtc] IS NOT NULL AND [ReplacedByTokenId] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ParentTokenId",
                        column: x => x.ParentTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    FirstSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    BrowserInfo = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TrustedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TrustedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TrustCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.CheckConstraint("CK_UserDevices_LastSeen_GTE_FirstSeen", "[LastSeenUtc] >= [FirstSeenUtc]");
                    table.CheckConstraint("CK_UserDevices_TrustRange", "([TrustedUntilUtc] IS NULL AND [TrustedAtUtc] IS NULL) OR ([TrustedUntilUtc] >= [TrustedAtUtc])");
                    table.ForeignKey(
                        name: "FK_UserDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMfa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretKeyEncrypted = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMfa", x => x.Id);
                    table.CheckConstraint("CK_UserMfa_Attempts_NonNegative", "[Attempts] >= 0");
                    table.CheckConstraint("CK_UserMfa_LockRange", "([LockedUntil] IS NULL AND [LockedAt] IS NULL) OR ([LockedUntil] >= [LockedAt])");
                    table.ForeignKey(
                        name: "FK_UserMfa_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.CheckConstraint("CK_UserPermissions_Updated_GTE_Created", "([UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt])");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "ActorId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Severity_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Severity", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Target_Action_Time",
                table: "AuditLogs",
                columns: new[] { "TargetEntityName", "TargetEntityId", "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Target_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TargetEntityName", "TargetEntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_CorrelationId",
                table: "IdentityOutboxMessages",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_LockedUntilUtc",
                table: "IdentityOutboxMessages",
                column: "LockedUntilUtc",
                filter: "[LockedUntilUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_MessageKey",
                table: "IdentityOutboxMessages",
                column: "MessageKey",
                unique: true,
                filter: "[MessageKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_OccurredAtUtc",
                table: "IdentityOutboxMessages",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Status_NextRetry_OccurredAt",
                table: "IdentityOutboxMessages",
                columns: new[] { "Status", "NextRetryUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Status_OccurredAtUtc",
                table: "IdentityOutboxMessages",
                columns: new[] { "Status", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_CorrelationId",
                table: "LoginAttempts",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Ip_Fail_Time",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "AttemptedAt" },
                filter: "[IpAddress] IS NOT NULL AND [Status] <> 1");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Ip_Status_Time",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "Status", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Login_Status_Time",
                table: "LoginAttempts",
                columns: new[] { "LoginNameOrEmailNormalized", "Status", "AttemptedAt" },
                filter: "[LoginNameOrEmailNormalized] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_LoginNorm_Fail_Time",
                table: "LoginAttempts",
                columns: new[] { "LoginNameOrEmailNormalized", "AttemptedAt" },
                filter: "[LoginNameOrEmailNormalized] IS NOT NULL AND [Status] <> 1");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Status_Time",
                table: "LoginAttempts",
                columns: new[] { "Status", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId",
                table: "LoginAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_Time",
                table: "LoginAttempts",
                columns: new[] { "UserId", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CreatedAt",
                table: "Permissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive",
                table: "Permissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_CreatedAt",
                table: "PhoneVerifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_ExpiresAt",
                table: "PhoneVerifications",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_LockedUntil",
                table: "PhoneVerifications",
                column: "LockedUntilUtc",
                filter: "[LockedUntilUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_Phone",
                table: "PhoneVerifications",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_PhoneNormalized",
                table: "PhoneVerifications",
                column: "PhoneNumberNormalized",
                filter: "[PhoneNumberNormalized] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PV_PhoneNormalized_Active",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumberNormalized", "IsVerified", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PV_Purpose_Channel_ExpiresAt",
                table: "PhoneVerifications",
                columns: new[] { "Purpose", "Channel", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PV_User_Active",
                table: "PhoneVerifications",
                columns: new[] { "UserId", "IsVerified", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PV_Active_Phone_Purpose_Channel",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumberNormalized", "Purpose", "Channel" },
                unique: true,
                filter: "[PhoneNumberNormalized] IS NOT NULL AND [IsVerified] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedAtUtc",
                table: "RefreshTokens",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ParentTokenId",
                table: "RefreshTokens",
                column: "ParentTokenId",
                filter: "[ParentTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId",
                filter: "[ReplacedByTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedAtUtc",
                table: "RefreshTokens",
                column: "RevokedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_User_Active",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc", "RevokedAtUtc" },
                filter: "[RevokedAtUtc] IS NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "TokenHash", "DeviceId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_User_Device_Active",
                table: "RefreshTokens",
                columns: new[] { "UserId", "DeviceId", "ExpiresAtUtc", "RevokedAtUtc" },
                filter: "[RevokedAtUtc] IS NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "TokenHash", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokens_User_Device_Active",
                table: "RefreshTokens",
                columns: new[] { "UserId", "DeviceId" },
                unique: true,
                filter: "[RevokedAtUtc] IS NULL AND [DeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_CreatedAt",
                table: "RolePermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedAt",
                table: "Roles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_AuditLogId",
                table: "SecurityEvents",
                column: "AuditLogId",
                filter: "[AuditLogId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CorrelationId",
                table: "SecurityEvents",
                column: "CorrelationId",
                filter: "[CorrelationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OccurredAt",
                table: "SecurityEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Type_Resolved_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "Type", "IsResolved", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Unresolved_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "IsResolved", "OccurredAt" },
                filter: "[IsResolved] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId_Type_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "UserId", "Type", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_LastSeenUtc",
                table: "UserDevices",
                column: "LastSeenUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_User_Active_LastSeen",
                table: "UserDevices",
                columns: new[] { "UserId", "IsActive", "LastSeenUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_User_IsActive",
                table: "UserDevices",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_User_TrustedUntil",
                table: "UserDevices",
                columns: new[] { "UserId", "TrustedUntilUtc" },
                filter: "[TrustedUntilUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_User_DeviceId",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_User_Fingerprint",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceFingerprint" },
                unique: true,
                filter: "[DeviceFingerprint] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfa_IsEnabled",
                table: "UserMfa",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfa_IsLocked",
                table: "UserMfa",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfa_LastVerifiedAt",
                table: "UserMfa",
                column: "LastVerifiedAt",
                filter: "[LastVerifiedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfa_LockedUntil",
                table: "UserMfa",
                column: "LockedUntil",
                filter: "[LockedUntil] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMfa_User_Enabled_Locked",
                table: "UserMfa",
                columns: new[] { "UserId", "IsEnabled", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "UX_UserMfa_UserId",
                table: "UserMfa",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_CreatedAt",
                table: "UserPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_User_Granted",
                table: "UserPermissions",
                columns: new[] { "UserId", "IsGranted" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Active",
                table: "Users",
                column: "Email",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone_Active",
                table: "Users",
                column: "PhoneNumber",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedPhone_Active",
                table: "Users",
                column: "NormalizedPhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0 AND [NormalizedPhoneNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "IdentityOutboxMessages");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "PhoneVerifications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserMfa");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
