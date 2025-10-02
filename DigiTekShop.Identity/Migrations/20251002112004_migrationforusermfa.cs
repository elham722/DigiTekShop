using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiTekShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class migrationforusermfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretKey",
                table: "UserMfa");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "UserMfa",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "UserMfa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                table: "UserMfa",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretKeyEncrypted",
                table: "UserMfa",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "UserMfa");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "UserMfa");

            migrationBuilder.DropColumn(
                name: "SecretKeyEncrypted",
                table: "UserMfa");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "UserMfa",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecretKey",
                table: "UserMfa",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
