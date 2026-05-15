using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class LinkedAccountIdsRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LinkedAccounts_BeatLeaderId",
                table: "Players",
                newName: "LinkedAccounts_SteamId");

            migrationBuilder.AlterColumn<decimal>(
                name: "LinkedAccounts_SteamId",
                table: "Players",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "LinkedAccounts_BLNativeId",
                table: "Players",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LinkedAccounts_MetaPCId",
                table: "Players",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedAccounts_BLNativeId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LinkedAccounts_MetaPCId",
                table: "Players");

            migrationBuilder.AlterColumn<decimal>(
                name: "LinkedAccounts_SteamId",
                table: "Players",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "LinkedAccounts_SteamId",
                table: "Players",
                newName: "LinkedAccounts_BeatLeaderId");
        }
    }
}
