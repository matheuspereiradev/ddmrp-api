using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBufferProfilesOnCenterProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdBufferProfile",
                table: "CenterProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdBufferProfile",
                table: "CenterProducts",
                column: "IdBufferProfile");

            migrationBuilder.AddForeignKey(
                name: "FK_CenterProducts_BufferProfiles_IdBufferProfile",
                table: "CenterProducts",
                column: "IdBufferProfile",
                principalTable: "BufferProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CenterProducts_BufferProfiles_IdBufferProfile",
                table: "CenterProducts");

            migrationBuilder.DropIndex(
                name: "IX_CenterProducts_IdBufferProfile",
                table: "CenterProducts");

            migrationBuilder.DropColumn(
                name: "IdBufferProfile",
                table: "CenterProducts");
        }
    }
}
