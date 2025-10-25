using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiTekShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class firstmig : Migration
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
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetEntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetEntityId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OldValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
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
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Type = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    NextRetryUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoginNameOrEmail = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LoginNameOrEmailNormalized = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true)
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                        onDelete: ReferentialAction.Cascade);
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
                    ReplacedByTokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    ParentTokenHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DeviceId = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokens_ExpiresAfterCreated", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_RefreshTokens_RevokedConsistency", "(([RevokedAtUtc] IS NULL AND [RevokedReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL))");
                    table.CheckConstraint("CK_RefreshTokens_RotationConsistency", "(([RotatedAtUtc] IS NULL AND [ReplacedByTokenHash] IS NULL) OR ([RotatedAtUtc] IS NOT NULL AND [ReplacedByTokenHash] IS NOT NULL))");
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
                    DeviceId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    FirstSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    BrowserInfo = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TrustedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TrustedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TrustCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
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
                    SecretKeyEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMfa", x => x.Id);
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
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
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

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
                name: "IX_AuditLogs_Entity_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TargetEntityName", "TargetEntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsSuccess",
                table: "AuditLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Severity",
                table: "AuditLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Severity_Success_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Severity", "IsSuccess", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TargetEntityName",
                table: "AuditLogs",
                column: "TargetEntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_CorrelationId",
                table: "IdentityOutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_LockedUntilUtc",
                table: "IdentityOutboxMessages",
                column: "LockedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Status_NextRetry_OccurredAt",
                table: "IdentityOutboxMessages",
                columns: new[] { "Status", "NextRetryUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Status_OccurredAtUtc",
                table: "IdentityOutboxMessages",
                columns: new[] { "Status", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Ip_Status_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "Status", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress",
                table: "LoginAttempts",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_LoginNameOrEmailNorm",
                table: "LoginAttempts",
                column: "LoginNameOrEmailNormalized",
                filter: "[LoginNameOrEmailNormalized] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Status_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Status", "AttemptedAt" })
                .Annotation("SqlServer:Include", new[] { "UserId", "IpAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "AttemptedAt" })
                .Annotation("SqlServer:Include", new[] { "Status", "IpAddress" });

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
                name: "IX_PhoneVerifications_Phone",
                table: "PhoneVerifications",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PV_Phone_Active",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "IsVerified", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PV_User_Active",
                table: "PhoneVerifications",
                columns: new[] { "UserId", "IsVerified", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PhoneVerifications_Phone_Code_ExpiresAt",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "CodeHash", "ExpiresAtUtc" },
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedAtUtc",
                table: "RefreshTokens",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAtUtc",
                table: "RefreshTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ParentTokenHash",
                table: "RefreshTokens",
                column: "ParentTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenHash",
                table: "RefreshTokens",
                column: "ReplacedByTokenHash");

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
                name: "UX_RolePermissions_Role_Permission",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

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
                name: "IX_SecurityEvents_DeviceId",
                table: "SecurityEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_DeviceId_Type_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "DeviceId", "Type", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IpAddress",
                table: "SecurityEvents",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IpAddress_Type_OccurredAt",
                table: "SecurityEvents",
                columns: new[] { "IpAddress", "Type", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IsResolved",
                table: "SecurityEvents",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OccurredAt",
                table: "SecurityEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Type",
                table: "SecurityEvents",
                column: "Type");

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
                name: "IX_UserDevices_User_IsActive",
                table: "UserDevices",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_User_TrustedUntil",
                table: "UserDevices",
                columns: new[] { "UserId", "TrustedUntilUtc" });

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
                name: "IX_UserMfa_LockedUntil",
                table: "UserMfa",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "UX_UserMfa_UserId",
                table: "UserMfa",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_IsGranted",
                table: "UserPermissions",
                column: "IsGranted");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PermissionId",
                table: "UserPermissions",
                columns: new[] { "UserId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Active",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail_Active",
                table: "Users",
                column: "NormalizedEmail",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone_Active",
                table: "Users",
                column: "PhoneNumber",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedPhone_Active",
                table: "Users",
                column: "NormalizedPhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0 AND [NormalizedPhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedUserName_Active",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[IsDeleted] = 0");
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
