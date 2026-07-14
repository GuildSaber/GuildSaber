using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class RankedScoreConfirmationFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscordInfo_ConfirmedScoreFeedChannelId",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscordInfo_RefusedScoreFeedChannelId",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordInfo_ConfirmedScoreFeedChannelId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "DiscordInfo_RefusedScoreFeedChannelId",
                table: "Guilds");
        }
    }
}
