using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAduFieldsToCenterProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Adu",
                table: "CenterProducts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FutureAduDays",
                table: "CenterProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoryAduDays",
                table: "CenterProducts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adu",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "FutureAduDays",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "HistoryAduDays",
                table: "CenterProducts");
        }
    }
}
