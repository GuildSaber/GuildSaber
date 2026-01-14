using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class IndexesOnRankedScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_PlayerId",
                table: "RankedScores");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PlayerId_State",
                table: "RankedScores",
                columns: new[] { "PlayerId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_State",
                table: "RankedScores",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_PlayerId_State",
                table: "RankedScores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_State",
                table: "RankedScores");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PlayerId",
                table: "RankedScores",
                column: "PlayerId");
        }
    }
}
