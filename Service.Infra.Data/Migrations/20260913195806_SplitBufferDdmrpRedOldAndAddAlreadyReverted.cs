using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitBufferDdmrpRedOldAndAddAlreadyReverted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BufferDdmrpRedOld",
                table: "BufferAdjustmentFactors",
                newName: "BufferDdmrpRedSafeOld");

            migrationBuilder.AddColumn<bool>(
                name: "AlreadyReverted",
                table: "BufferAdjustmentFactors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BufferDdmrpRedBaseOld",
                table: "BufferAdjustmentFactors",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlreadyReverted",
                table: "BufferAdjustmentFactors");

            migrationBuilder.DropColumn(
                name: "BufferDdmrpRedBaseOld",
                table: "BufferAdjustmentFactors");

            migrationBuilder.RenameColumn(
                name: "BufferDdmrpRedSafeOld",
                table: "BufferAdjustmentFactors",
                newName: "BufferDdmrpRedOld");
        }
    }
}
