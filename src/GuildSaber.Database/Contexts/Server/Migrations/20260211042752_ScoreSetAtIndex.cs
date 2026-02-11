using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class ScoreSetAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_RankedMapId",
                table: "RankedScores");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_SetAt",
                table: "Scores",
                column: "SetAt");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_State",
                table: "RankedScores",
                columns: new[] { "RankedMapId", "PlayerId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_SetAt",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_State",
                table: "RankedScores");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_RankedMapId",
                table: "RankedScores",
                column: "RankedMapId");
        }
    }
}
