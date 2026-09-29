using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReasonGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdReasonGroup",
                table: "Reasons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReasonGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsFromSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonGroups", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Module" },
                values: new object[] { "reasongroup:GET", "List reason groups", "ReasonGroup" });

            migrationBuilder.CreateIndex(
                name: "IX_Reasons_IdReasonGroup",
                table: "Reasons",
                column: "IdReasonGroup");

            migrationBuilder.AddForeignKey(
                name: "FK_Reasons_ReasonGroups_IdReasonGroup",
                table: "Reasons",
                column: "IdReasonGroup",
                principalTable: "ReasonGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reasons_ReasonGroups_IdReasonGroup",
                table: "Reasons");

            migrationBuilder.DropTable(
                name: "ReasonGroups");

            migrationBuilder.DropIndex(
                name: "IX_Reasons_IdReasonGroup",
                table: "Reasons");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: "reasongroup:GET");

            migrationBuilder.DropColumn(
                name: "IdReasonGroup",
                table: "Reasons");
        }
    }
}
