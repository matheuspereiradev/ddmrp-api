using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCenterAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAllCentersAccess",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUser = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCenters_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCenters_Users_IdUser",
                        column: x => x.IdUser,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[,]
                {
                    { "user/all-centers-access:PATCH", "Set user access to all centers", "User" },
                    { "user/centers:GET", "List centers a user has access to", "User" },
                    { "user/centers:PUT", "Replace centers a user has access to", "User" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCenters_IdCenter",
                table: "UserCenters",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_UserCenters_IdUser_IdCenter",
                table: "UserCenters",
                columns: new[] { "IdUser", "IdCenter" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCenters");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user/all-centers-access:PATCH");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user/centers:GET");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "user/centers:PUT");

            migrationBuilder.DropColumn(
                name: "HasAllCentersAccess",
                table: "Users");
        }
    }
}
