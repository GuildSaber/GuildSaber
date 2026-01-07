using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class DiscordInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordInfo_DiscordInviteCode",
                table: "Guilds",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordInfo_DiscordInviteCode",
                table: "Guilds");
        }
    }
}
