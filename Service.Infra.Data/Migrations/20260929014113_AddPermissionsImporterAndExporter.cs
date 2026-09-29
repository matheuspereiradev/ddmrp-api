using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsImporterAndExporter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "exporter:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "exporter/download:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "exporter/params:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "exporter/run:POST");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "importer:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "importer/params:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "importer/run:POST");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[,]
                {
                    { "exporter", "List, inspect, run and download exporters", "Exporter" },
                    { "importer", "List, inspect and run importers", "Importer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "exporter");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "importer");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[,]
                {
                    { "exporter:GET", "List exporters", "Exporter" },
                    { "exporter/download:GET", "Download exporter result file", "Exporter" },
                    { "exporter/params:GET", "View exporter procedure parameters", "Exporter" },
                    { "exporter/run:POST", "Run exporter", "Exporter" },
                    { "importer:GET", "List importers", "Importer" },
                    { "importer/params:GET", "View importer procedure parameters", "Importer" },
                    { "importer/run:POST", "Run importer", "Importer" }
                });
        }
    }
}
