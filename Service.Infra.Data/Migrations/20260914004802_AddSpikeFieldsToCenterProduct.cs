using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpikeFieldsToCenterProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "QualifiedDemand",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpikeHorizonLTDays",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "SpikeHorizonType",
                table: "CenterProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SpikeHorizonValue",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 60m);

            migrationBuilder.AddColumn<decimal>(
                name: "SpikeThresholdAdu",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "SpikeThresholdPercentageRedZone",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0.5m);

            migrationBuilder.AddColumn<int>(
                name: "SpikeThresholdType",
                table: "CenterProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QualifiedDemand",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeHorizonLTDays",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeHorizonType",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeHorizonValue",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeThresholdAdu",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeThresholdPercentageRedZone",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "SpikeThresholdType",
                table: "CenterProducts");
        }
    }
}
