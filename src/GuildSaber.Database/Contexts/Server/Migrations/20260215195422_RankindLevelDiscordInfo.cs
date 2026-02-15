using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class RankindLevelDiscordInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscordInfo_RoleId",
                table: "Levels",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordInfo_RoleId",
                table: "Levels");
        }
    }
}
