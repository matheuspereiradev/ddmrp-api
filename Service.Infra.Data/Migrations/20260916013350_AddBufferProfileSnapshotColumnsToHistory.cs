using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBufferProfileSnapshotColumnsToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Adi",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Adu",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cv",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FutureAduDays",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HistoryAduDays",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdBufferProfile",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdReason",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTag",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTime",
                table: "Histories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Moq",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenInbounds",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpenOutbound",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackQuantity",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QualifiedDemand",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedBaseZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RedSafeZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardDeviation",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Stock",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YellowZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ZafGreenZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ZafRedZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ZafYellowZone",
                table: "Histories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Histories_IdBufferProfile",
                table: "Histories",
                column: "IdBufferProfile");

            migrationBuilder.AddForeignKey(
                name: "FK_Histories_BufferProfiles_IdBufferProfile",
                table: "Histories",
                column: "IdBufferProfile",
                principalTable: "BufferProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Histories_BufferProfiles_IdBufferProfile",
                table: "Histories");

            migrationBuilder.DropIndex(
                name: "IX_Histories_IdBufferProfile",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Adi",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Adu",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Cv",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "FutureAduDays",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "GreenZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "HistoryAduDays",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "IdBufferProfile",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "IdReason",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "IdTag",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "LeadTime",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Moq",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "OpenInbounds",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "OpenOutbound",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "PackQuantity",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "QualifiedDemand",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "RedBaseZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "RedSafeZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "StandardDeviation",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "YellowZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "ZafGreenZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "ZafRedZone",
                table: "Histories");

            migrationBuilder.DropColumn(
                name: "ZafYellowZone",
                table: "Histories");
        }
    }
}
