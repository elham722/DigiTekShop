using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiTekShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class foruserconfirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAtUtc",
                table: "Users",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted_CreatedAtUtc",
                table: "Users",
                columns: new[] { "IsDeleted", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LockoutEnd_Active",
                table: "Users",
                column: "LockoutEnd",
                filter: "[LockoutEnd] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAtUtc",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsDeleted_CreatedAtUtc",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LockoutEnd_Active",
                table: "Users");
        }
    }
}
