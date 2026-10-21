using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastAIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    MonthYear = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviewType = table.Column<int>(type: "int", nullable: false),
                    Assertiveness = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityPreviewed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreviewState = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastAIs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastAIs_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ForecastAIs_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastAIs_IdCenter",
                table: "ForecastAIs",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastAIs_IdProduct",
                table: "ForecastAIs",
                column: "IdProduct");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastAIs");
        }
    }
}
