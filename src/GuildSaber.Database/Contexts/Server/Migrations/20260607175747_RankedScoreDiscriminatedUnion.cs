using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations
{
    /// <inheritdoc />
    public partial class RankedScoreDiscriminatedUnion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_PlayerId_State",
                table: "RankedScores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_State",
                table: "RankedScores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_State",
                table: "RankedScores");

            migrationBuilder.AlterColumn<float>(
                name: "RawPoints",
                table: "RankedScores",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "Rank",
                table: "RankedScores",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "InvalidReason",
                table: "RankedScores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "RankedScores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "Type",
                table: "RankedScores",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(
                """
                WITH mapped AS (
                    SELECT
                        "Id",
                        (("State" & 1) = 1) AS "NewIsSelected",
                        CASE
                            WHEN "DenyReason" <> 0 OR ("State" & 2) = 2 OR ("State" & 4) = 4 THEN 1
                            WHEN ("State" & 16) = 16 THEN 3
                            WHEN ("State" & 32) = 32 THEN 4
                            WHEN ("State" & 8) = 8 THEN 2
                            ELSE 0
                        END::smallint AS "NewType",
                        CASE
                            WHEN "DenyReason" <> 0 OR ("State" & 2) = 2 OR ("State" & 4) = 4 THEN "DenyReason"
                            ELSE NULL
                        END AS "NewInvalidReason"
                    FROM "RankedScores"
                )
                UPDATE "RankedScores" rs
                SET
                    "IsSelected" = mapped."NewIsSelected",
                    "Type" = mapped."NewType",
                    "InvalidReason" = mapped."NewInvalidReason",
                    "RawPoints" = CASE WHEN mapped."NewType" = 1 THEN NULL ELSE rs."RawPoints" END,
                    "Rank" = CASE WHEN mapped."NewType" IN (0, 3) THEN rs."Rank" ELSE NULL END
                FROM mapped
                WHERE rs."Id" = mapped."Id";
                """);

            migrationBuilder.DropColumn(
                name: "DenyReason",
                table: "RankedScores");

            migrationBuilder.DropColumn(
                name: "State",
                table: "RankedScores");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_IsSelected",
                table: "RankedScores",
                column: "IsSelected");

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PlayerId_IsSelected",
                table: "RankedScores",
                columns: new[] { "PlayerId", "IsSelected" });

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_IsSelected",
                table: "RankedScores",
                columns: new[] { "RankedMapId", "PlayerId", "IsSelected" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RankedScores_IsSelected",
                table: "RankedScores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_PlayerId_IsSelected",
                table: "RankedScores");

            migrationBuilder.DropIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_IsSelected",
                table: "RankedScores");

            migrationBuilder.AddColumn<int>(
                name: "DenyReason",
                table: "RankedScores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "RankedScores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE "RankedScores"
                SET
                    "DenyReason" = CASE WHEN "Type" = 1 THEN COALESCE("InvalidReason", 0) ELSE 0 END,
                    "State" = (
                        CASE WHEN "IsSelected" THEN 1 ELSE 0 END
                        | CASE "Type"
                            WHEN 1 THEN 2
                            WHEN 2 THEN 8
                            WHEN 3 THEN 16
                            WHEN 4 THEN 32
                            ELSE 0
                        END
                    ),
                    "RawPoints" = COALESCE("RawPoints", 0),
                    "Rank" = COALESCE("Rank", 0);
                """);

            migrationBuilder.DropColumn(
                name: "InvalidReason",
                table: "RankedScores");

            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "RankedScores");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "RankedScores");

            migrationBuilder.AlterColumn<float>(
                name: "RawPoints",
                table: "RankedScores",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Rank",
                table: "RankedScores",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_PlayerId_State",
                table: "RankedScores",
                columns: new[] { "PlayerId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_RankedMapId_PlayerId_State",
                table: "RankedScores",
                columns: new[] { "RankedMapId", "PlayerId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_RankedScores_State",
                table: "RankedScores",
                column: "State");
        }
    }
}
