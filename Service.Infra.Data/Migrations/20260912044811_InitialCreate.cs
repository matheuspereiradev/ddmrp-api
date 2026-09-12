using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllocationGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BufferProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SupplyType = table.Column<int>(type: "int", nullable: false),
                    LeadTimeCategory = table.Column<int>(type: "int", nullable: false),
                    VariabilityCategory = table.Column<int>(type: "int", nullable: false),
                    LeadTimeFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VariabilityFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AduCalculationDays = table.Column<int>(type: "int", nullable: false),
                    AduFutureDays = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    UseAdUxDlTxFactorDlt = table.Column<bool>(type: "bit", nullable: false),
                    UseMoq = table.Column<bool>(type: "bit", nullable: false),
                    UseAdUxFrequency = table.Column<bool>(type: "bit", nullable: false),
                    SpikeHorizonType = table.Column<int>(type: "int", nullable: false),
                    SpikeHorizonValue = table.Column<int>(type: "int", nullable: false),
                    SpikeHorizonLTDays = table.Column<int>(type: "int", nullable: false),
                    SpikeThresholdType = table.Column<int>(type: "int", nullable: false),
                    SpikeThresholdAdu = table.Column<int>(type: "int", nullable: false),
                    SpikeThresholdPercentageRedZone = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsMakeToOrder = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BufferProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Centers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Centers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuxiliarMaterialCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Segment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Pallet = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Line = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subline = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ABC = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkCenter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Reasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BufferAdjustmentFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BufferType = table.Column<int>(type: "int", nullable: false),
                    BufferDdmrpRed = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BufferDdmrpYellow = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BufferDdmrpGreen = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Obs = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BufferTypeOld = table.Column<int>(type: "int", nullable: true),
                    BufferDdmrpRedOld = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BufferDdmrpYellowOld = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BufferDdmrpGreenOld = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BufferAdjustmentFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BufferAdjustmentFactors_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BufferAdjustmentFactors_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandAdjustmentFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Obs = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAdjustmentFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandAdjustmentFactors_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandAdjustmentFactors_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Forecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forecasts_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Forecasts_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Histories_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Histories_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasterBuffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    IdProductFather = table.Column<int>(type: "int", nullable: false),
                    IdCenterFather = table.Column<int>(type: "int", nullable: false),
                    Sequency = table.Column<int>(type: "int", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ZoneAdjustmentFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    TargetZone = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Obs = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneAdjustmentFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZoneAdjustmentFactors_Centers_IdCenter",
                        column: x => x.IdCenter,
                        principalTable: "Centers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZoneAdjustmentFactors_Products_IdProduct",
                        column: x => x.IdProduct,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IdRole = table.Column<int>(type: "int", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_IdRole",
                        column: x => x.IdRole,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CenterProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProduct = table.Column<int>(type: "int", nullable: false),
                    IdCenter = table.Column<int>(type: "int", nullable: false),
                    IdOriginCenter = table.Column<int>(type: "int", nullable: true),
                    PackQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Moq = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LeadTime = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Segment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Stock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IdProvider = table.Column<int>(type: "int", nullable: true),
                    IdTag = table.Column<int>(type: "int", nullable: true),
                    IdReason = table.Column<int>(type: "int", nullable: true),
                    IdAllocationGroup = table.Column<int>(type: "int", nullable: true),
                    IdBufferProfile = table.Column<int>(type: "int", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
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
                        name: "FK_CenterProducts_BufferProfiles_IdBufferProfile",
                        column: x => x.IdBufferProfile,
                        principalTable: "BufferProfiles",
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

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CenterProductId = table.Column<int>(type: "int", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdBy = table.Column<int>(type: "int", nullable: true),
                    updatedBy = table.Column<int>(type: "int", nullable: true),
                    deletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_CenterProducts_CenterProductId",
                        column: x => x.CenterProductId,
                        principalTable: "CenterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Users_createdBy",
                        column: x => x.createdBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BufferAdjustmentFactors_IdCenter",
                table: "BufferAdjustmentFactors",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_BufferAdjustmentFactors_IdProduct",
                table: "BufferAdjustmentFactors",
                column: "IdProduct");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdAllocationGroup",
                table: "CenterProducts",
                column: "IdAllocationGroup");

            migrationBuilder.CreateIndex(
                name: "IX_CenterProducts_IdBufferProfile",
                table: "CenterProducts",
                column: "IdBufferProfile");

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

            migrationBuilder.CreateIndex(
                name: "IX_DemandAdjustmentFactors_IdCenter",
                table: "DemandAdjustmentFactors",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAdjustmentFactors_IdProduct",
                table: "DemandAdjustmentFactors",
                column: "IdProduct");

            migrationBuilder.CreateIndex(
                name: "IX_Forecasts_IdCenter",
                table: "Forecasts",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_Forecasts_IdProduct",
                table: "Forecasts",
                column: "IdProduct");

            migrationBuilder.CreateIndex(
                name: "IX_Histories_IdCenter",
                table: "Histories",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_Histories_IdProduct",
                table: "Histories",
                column: "IdProduct");

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

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CenterProductId",
                table: "Notes",
                column: "CenterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_createdBy",
                table: "Notes",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdRole",
                table: "Users",
                column: "IdRole");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneAdjustmentFactors_IdCenter",
                table: "ZoneAdjustmentFactors",
                column: "IdCenter");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneAdjustmentFactors_IdProduct",
                table: "ZoneAdjustmentFactors",
                column: "IdProduct");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BufferAdjustmentFactors");

            migrationBuilder.DropTable(
                name: "DemandAdjustmentFactors");

            migrationBuilder.DropTable(
                name: "Forecasts");

            migrationBuilder.DropTable(
                name: "Histories");

            migrationBuilder.DropTable(
                name: "MasterBuffers");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ZoneAdjustmentFactors");

            migrationBuilder.DropTable(
                name: "CenterProducts");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AllocationGroups");

            migrationBuilder.DropTable(
                name: "BufferProfiles");

            migrationBuilder.DropTable(
                name: "Centers");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Reasons");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
