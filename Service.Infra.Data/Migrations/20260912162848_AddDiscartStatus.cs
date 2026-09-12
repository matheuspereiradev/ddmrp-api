using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscartStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscardStatus",
                table: "Histories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscardStatus",
                table: "Histories");
        }
    }
}
