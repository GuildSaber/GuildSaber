using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuildSaber.Database.Contexts.DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlexHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GlobalLevelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlexHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlexHistoryLevelStats",
                columns: table => new
                {
                    FlexHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    LevelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlexHistoryLevelStats", x => new { x.FlexHistoryId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_FlexHistoryLevelStats_FlexHistories_FlexHistoryId",
                        column: x => x.FlexHistoryId,
                        principalTable: "FlexHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlexHistoryPointStats",
                columns: table => new
                {
                    FlexHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    PointId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlexHistoryPointStats", x => new { x.FlexHistoryId, x.PointId });
                    table.ForeignKey(
                        name: "FK_FlexHistoryPointStats_FlexHistories_FlexHistoryId",
                        column: x => x.FlexHistoryId,
                        principalTable: "FlexHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlexHistories_GuildId_ContextId_Timestamp",
                table: "FlexHistories",
                columns: new[] { "GuildId", "ContextId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FlexHistories_PlayerId_GuildId_ContextId_Timestamp",
                table: "FlexHistories",
                columns: new[] { "PlayerId", "GuildId", "ContextId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FlexHistoryPointStats_FlexHistoryId_PointId",
                table: "FlexHistoryPointStats",
                columns: new[] { "FlexHistoryId", "PointId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlexHistoryLevelStats");

            migrationBuilder.DropTable(
                name: "FlexHistoryPointStats");

            migrationBuilder.DropTable(
                name: "FlexHistories");
        }
    }
}
