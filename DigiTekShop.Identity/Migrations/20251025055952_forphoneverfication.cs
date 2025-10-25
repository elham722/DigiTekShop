using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiTekShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class forphoneverfication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhoneVerifications_Phone_CreatedAt",
                table: "PhoneVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PhoneVerifications_Phone_ExpiresAt",
                table: "PhoneVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PhoneVerifications_Phone_IsVerified",
                table: "PhoneVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PhoneVerifications_UserId",
                table: "PhoneVerifications");

            migrationBuilder.AddColumn<string>(
                name: "CodeHashAlgo",
                table: "PhoneVerifications",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "PhoneVerifications",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedCodeProtected",
                table: "PhoneVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecretVersion",
                table: "PhoneVerifications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_PV_Phone_Active",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "IsVerified", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PV_User_Active",
                table: "PhoneVerifications",
                columns: new[] { "UserId", "IsVerified", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PV_Phone_Active",
                table: "PhoneVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PV_User_Active",
                table: "PhoneVerifications");

            migrationBuilder.DropColumn(
                name: "CodeHashAlgo",
                table: "PhoneVerifications");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "PhoneVerifications");

            migrationBuilder.DropColumn(
                name: "EncryptedCodeProtected",
                table: "PhoneVerifications");

            migrationBuilder.DropColumn(
                name: "SecretVersion",
                table: "PhoneVerifications");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_Phone_CreatedAt",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_Phone_ExpiresAt",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_Phone_IsVerified",
                table: "PhoneVerifications",
                columns: new[] { "PhoneNumber", "IsVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneVerifications_UserId",
                table: "PhoneVerifications",
                column: "UserId");
        }
    }
}
