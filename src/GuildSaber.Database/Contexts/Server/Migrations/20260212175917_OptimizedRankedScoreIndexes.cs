using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class OptimizedRankedScoreIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_ContextId_PlayerId_RawPoints_Id",
                table: "RankedScores",
                columns: new[] { "ContextId", "PlayerId", "RawPoints", "Id" },
                descending: new[] { false, false, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_ContextId_PlayerId_RawPoints_Id",
                table: "RankedScores");
        }
    }
}
