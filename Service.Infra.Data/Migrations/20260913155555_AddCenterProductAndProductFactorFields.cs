using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCenterProductAndProductFactorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomLeadTimeFactor",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomVariabilityFactor",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);
            migrationBuilder.AddColumn<decimal>(
                name: "RedZoneBase",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedZoneSafe",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YellowZone",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenZone",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);


            migrationBuilder.AddColumn<bool>(
                name: "UseDafOnGreenZone",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseSuggestedLTFactor",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseSuggestedVariabilityFactor",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomLeadTimeFactor",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "CustomVariabilityFactor",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "GreenZone",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "RedZoneBase",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "RedZoneSafe",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "UseDafOnGreenZone",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "UseSuggestedLTFactor",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "UseSuggestedVariabilityFactor",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "YellowZone",
                table: "CenterProducts");
        }
    }
}
