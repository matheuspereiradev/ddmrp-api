using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestructureForecastAddSettingAndCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Forecasts",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Forecasts",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Forecasts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Calendar",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calendar", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MondayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TuesdayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    WednesdayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ThursdayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FridayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SaturdayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SundayIsWorkingDay = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "FridayIsWorkingDay", "MondayIsWorkingDay", "ThursdayIsWorkingDay", "TuesdayIsWorkingDay", "WednesdayIsWorkingDay", "createdBy", "deletedAt", "deletedBy", "updatedAt", "updatedBy" },
                values: new object[] { 1, true, true, true, true, true, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calendar");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Forecasts");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Forecasts",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Forecasts",
                newName: "Date");
        }
    }
}
