using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterBuffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterBuffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProduct = table.Column<int>(type: "integer", nullable: false),
                    IdCenter = table.Column<int>(type: "integer", nullable: false),
                    IdProductFather = table.Column<int>(type: "integer", nullable: false),
                    IdCenterFather = table.Column<int>(type: "integer", nullable: false),
                    Sequency = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdBy = table.Column<int>(type: "integer", nullable: true),
                    updatedBy = table.Column<int>(type: "integer", nullable: true),
                    deletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterBuffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterBuffers_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterBuffers_Centers_IdCenterFather",
                        column: x => x.IdCenterFather,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterBuffers_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterBuffers_Products_IdProductFather",
                        column: x => x.IdProductFather,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterBuffers_IdCenter",
                table: "MasterBuffers",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBuffers_IdCenterFather",
                table: "MasterBuffers",
                column: "IdCenterFather");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBuffers_IdProduct",
                table: "MasterBuffers",
                column: "IdProduct");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBuffers_IdProductFather",
                table: "MasterBuffers",
                column: "IdProductFather");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterBuffers");
        }
    }
}
