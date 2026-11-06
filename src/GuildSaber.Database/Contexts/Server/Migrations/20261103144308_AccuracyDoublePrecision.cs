using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildSaber.Database.Contexts.Server.Migrations;

/// <inheritdoc />
public partial class _20261103144308_AccuracyDoublePrecision : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<double>(
            name: "Requirements_MinAccuracy",
            table: "RankedMaps",
            type: "double precision",
            nullable: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldNullable: true);

        migrationBuilder.AlterColumn<double>(
            name: "Requirements_MinAccuracy",
            table: "MapProposals",
            type: "double precision",
            nullable: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<float>(
            name: "Requirements_MinAccuracy",
            table: "RankedMaps",
            type: "real",
            nullable: true,
            oldClrType: typeof(double),
            oldType: "double precision",
            oldNullable: true);

        migrationBuilder.AlterColumn<float>(
            name: "Requirements_MinAccuracy",
            table: "MapProposals",
            type: "real",
            nullable: true,
            oldClrType: typeof(double),
            oldType: "double precision",
            oldNullable: true);
    }
}
