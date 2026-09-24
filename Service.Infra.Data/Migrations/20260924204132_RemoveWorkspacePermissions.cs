using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkspacePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "workspace:DELETE");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "workspace:PUT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[,]
                {
                    { "workspace:DELETE", "Clear workspace", "Workspace" },
                    { "workspace:PUT", "Update workspace", "Workspace" }
                });
        }
    }
}
