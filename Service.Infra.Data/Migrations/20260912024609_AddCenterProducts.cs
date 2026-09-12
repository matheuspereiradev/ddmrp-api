using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCenterProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CenterProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdProduct = table.Column<int>(type: "integer", nullable: false),
                    IdCenter = table.Column<int>(type: "integer", nullable: false),
                    IdOriginCenter = table.Column<int>(type: "integer", nullable: true),
                    PackQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Moq = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LeadTime = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    Class = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Classification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Segment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IdProvider = table.Column<int>(type: "integer", nullable: true),
                    IdTag = table.Column<int>(type: "integer", nullable: true),
                    IdReason = table.Column<int>(type: "integer", nullable: true),
                    IdAllocationGroup = table.Column<int>(type: "integer", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdBy = table.Column<int>(type: "integer", nullable: true),
                    updatedBy = table.Column<int>(type: "integer", nullable: true),
                    deletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenterProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenterProducts_AllocationGroups_IdAllocationGroup",
                        column: x => x.IdAllocationGroup,
                        principalTable: "AllocationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Centers_IdOriginCenter",
                        column: x => x.IdOriginCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Partners_IdProvider",
                        column: x => x.IdProvider,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Reasons_IdReason",
                        column: x => x.IdReason,
                        principalTable: "Reasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenterProducts_Tags_IdTag",
                        column: x => x.IdTag,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdAllocationGroup",
                table: "CenterProducts",
                column: "IdAllocationGroup");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdCenter",
                table: "CenterProducts",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdOriginCenter",
                table: "CenterProducts",
                column: "IdOriginCenter");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdProduct",
                table: "CenterProducts",
                column: "IdProduct");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdProvider",
                table: "CenterProducts",
                column: "IdProvider");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdReason",
                table: "CenterProducts",
                column: "IdReason");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdTag",
                table: "CenterProducts",
                column: "IdTag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CenterProducts");
        }
    }
}
