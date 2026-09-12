using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBufferProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BufferProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileName = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SupplyType = table.Column<int>(type: "integer", nullable: false),
                    LeadTimeCategory = table.Column<int>(type: "integer", nullable: false),
                    VariabilityCategory = table.Column<int>(type: "integer", nullable: false),
                    LeadTimeFactor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VariabilityFactor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AduCalculationDays = table.Column<int>(type: "integer", nullable: false),
                    AduFutureDays = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    UseAdUxDlTxFactorDlt = table.Column<bool>(type: "boolean", nullable: false),
                    UseMoq = table.Column<bool>(type: "boolean", nullable: false),
                    UseAdUxFrequency = table.Column<bool>(type: "boolean", nullable: false),
                    SpikeHorizonType = table.Column<int>(type: "integer", nullable: false),
                    SpikeHorizonValue = table.Column<int>(type: "integer", nullable: false),
                    SpikeHorizonLTDays = table.Column<int>(type: "integer", nullable: false),
                    SpikeThresholdType = table.Column<int>(type: "integer", nullable: false),
                    SpikeThresholdAdu = table.Column<int>(type: "integer", nullable: false),
                    SpikeThresholdPercentageRedZone = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsMakeToOrder = table.Column<bool>(type: "boolean", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdBy = table.Column<int>(type: "integer", nullable: true),
                    updatedBy = table.Column<int>(type: "integer", nullable: true),
                    deletedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BufferProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BufferProfiles");
        }
    }
}
