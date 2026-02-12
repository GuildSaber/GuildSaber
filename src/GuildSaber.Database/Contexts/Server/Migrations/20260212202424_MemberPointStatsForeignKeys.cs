using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class MemberPointStatsForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MemberPointStats_PlayerId",
                table: "MemberPointStats",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPointStats_Contexts_ContextId",
                table: "MemberPointStats",
                column: "ContextId",
                principalTable: "Contexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPointStats_Guilds_GuildId",
                table: "MemberPointStats",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberPointStats_Players_PlayerId",
                table: "MemberPointStats",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberPointStats_Contexts_ContextId",
                table: "MemberPointStats");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberPointStats_Guilds_GuildId",
                table: "MemberPointStats");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberPointStats_Players_PlayerId",
                table: "MemberPointStats");

            migrationBuilder.DropIndex(
                name: "IX_MemberPointStats_PlayerId",
                table: "MemberPointStats");
        }
    }
}
