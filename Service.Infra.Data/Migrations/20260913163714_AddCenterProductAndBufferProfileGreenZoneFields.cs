using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCenterProductAndBufferProfileGreenZoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UseMoq",
                table: "BufferProfiles",
                newName: "GreenZoneParametrizationUseMoq");

            migrationBuilder.RenameColumn(
                name: "UseAdUxFrequency",
                table: "BufferProfiles",
                newName: "GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime");

            migrationBuilder.RenameColumn(
                name: "UseAdUxDlTxFactorDlt",
                table: "BufferProfiles",
                newName: "GreenZoneParametrizationUseAduXFrequency");

            migrationBuilder.AddColumn<int>(
                name: "BufferType",
                table: "CenterProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "GreenZoneParametrizationUseAduXFrequency",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "GreenZoneParametrizationUseMoq",
                table: "CenterProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BufferType",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "GreenZoneParametrizationUseAduXFrequency",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "GreenZoneParametrizationUseMoq",
                table: "CenterProducts");

            migrationBuilder.RenameColumn(
                name: "GreenZoneParametrizationUseMoq",
                table: "BufferProfiles",
                newName: "UseMoq");

            migrationBuilder.RenameColumn(
                name: "GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime",
                table: "BufferProfiles",
                newName: "UseAdUxFrequency");

            migrationBuilder.RenameColumn(
                name: "GreenZoneParametrizationUseAduXFrequency",
                table: "BufferProfiles",
                newName: "UseAdUxDlTxFactorDlt");
        }
    }
}
