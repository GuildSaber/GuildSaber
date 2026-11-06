using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations;

/// <inheritdoc />
public partial class _20261029103117_MapPointAccuracyCurves : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "CurveSettings_AccuracyBonusRatio",
            table: "Points",
            type: "double precision",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<byte>(
            name: "CurveSettings_MapCurvePolicy",
            table: "Points",
            type: "smallint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.AddColumn<double[]>(
            name: "CurveSettings_RunnerUpRewardFractions",
            table: "Points",
            type: "double precision[]",
            nullable: false,
            defaultValue: new double[0]);

        migrationBuilder.CreateTable(
            name: "RankedMapPointCurves",
            columns: table => new
            {
                RankedMapId = table.Column<long>(type: "bigint", nullable: false),
                PointId = table.Column<int>(type: "integer", nullable: false),
                Curve = table.Column<NpgsqlPoint[]>(type: "point[]", nullable: false),
                BenchmarkAccuracies = table.Column<double[]>(type: "double precision[]", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RankedMapPointCurves", x => new { x.RankedMapId, x.PointId });
                table.ForeignKey(
                    name: "FK_RankedMapPointCurves_Points_PointId",
                    column: x => x.PointId,
                    principalTable: "Points",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RankedMapPointCurves_RankedMaps_RankedMapId",
                    column: x => x.RankedMapId,
                    principalTable: "RankedMaps",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RankedMapPointCurves_PointId_RankedMapId",
            table: "RankedMapPointCurves",
            columns: new[] { "PointId", "RankedMapId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RankedMapPointCurves");

        migrationBuilder.DropColumn(
            name: "CurveSettings_AccuracyBonusRatio",
            table: "Points");

        migrationBuilder.DropColumn(
            name: "CurveSettings_MapCurvePolicy",
            table: "Points");

        migrationBuilder.DropColumn(
            name: "CurveSettings_RunnerUpRewardFractions",
            table: "Points");
    }
}
