using Microsoft.EntityFrameworkCore;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;
using Service.Domain.Utils;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;

        public ReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryBufferManagementRow>> GetInventoryBufferManagementAsync(CancellationToken cancellationToken = default)
        {
            var rows = await _context.CenterProduct
                .Where(cp => cp.deletedAt == null && cp.Center.deletedAt == null && cp.Product.deletedAt == null)
                .Select(cp => new InventoryBufferManagementRow
                {
                    Id = cp.Id,
                    IdProduct = cp.IdProduct,
                    IdCenter = cp.IdCenter,
                    IdOriginCenter = cp.IdOriginCenter,
                    PackQuantity = cp.PackQuantity,
                    Moq = cp.Moq,
                    LeadTime = cp.LeadTime,
                    Frequency = cp.Frequency,
                    Class = cp.Class,
                    Classification = cp.Classification,
                    Segment = cp.Segment,
                    Stock = cp.Stock,
                    IdProvider = cp.IdProvider,
                    IdTag = cp.IdTag,
                    IdReason = cp.IdReason,
                    IdAllocationGroup = cp.IdAllocationGroup,
                    IdBufferProfile = cp.IdBufferProfile,
                    Adu = cp.Adu,
                    FutureAduDays = cp.FutureAduDays,
                    HistoryAduDays = cp.HistoryAduDays,
                    Adi = cp.Adi,
                    StandardDeviation = cp.StandardDeviation,
                    Cv = cp.Cv,
                    UseSuggestedLTFactor = cp.UseSuggestedLTFactor,
                    UseSuggestedVariabilityFactor = cp.UseSuggestedVariabilityFactor,
                    RedZoneBase = cp.RedZoneBase,
                    RedZoneSafe = cp.RedZoneSafe,
                    RedZone = cp.RedZone,
                    YellowZone = cp.YellowZone,
                    GreenZone = cp.GreenZone,
                    TopOfRed = cp.TopOfRed,
                    TopOfYellow = cp.TopOfYellow,
                    TopOfGreen = cp.TopOfGreen,
                    RedZoneExecution = cp.RedZoneExecution,
                    YellowZoneExecution = cp.YellowZoneExecution,
                    GreenZoneExecution = cp.GreenZoneExecution,
                    RedSafeAnalytical = cp.RedSafeAnalytical,
                    YellowSafeAnalytical = cp.YellowSafeAnalytical,
                    GreenAnalytical = cp.GreenAnalytical,
                    YellowExcessAnalytical = cp.YellowExcessAnalytical,
                    RedSafeExcessAnalytical = cp.RedSafeExcessAnalytical,
                    UseDafOnGreenZone = cp.UseDafOnGreenZone,
                    CustomLeadTimeFactor = cp.CustomLeadTimeFactor,
                    CustomVariabilityFactor = cp.CustomVariabilityFactor,
                    GreenZoneParametrizationUseMoq = cp.GreenZoneParametrizationUseMoq,
                    GreenZoneParametrizationUseAduXFrequency = cp.GreenZoneParametrizationUseAduXFrequency,
                    GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = cp.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
                    BufferType = cp.BufferType,
                    ZafRedZone = cp.ZafRedZone,
                    ZafYellowZone = cp.ZafYellowZone,
                    ZafGreenZone = cp.ZafGreenZone,
                    QualifiedDemand = cp.QualifiedDemand,
                    SpikeHorizonType = cp.SpikeHorizonType,
                    SpikeHorizonValue = cp.SpikeHorizonValue,
                    SpikeHorizonLTDays = cp.SpikeHorizonLTDays,
                    SpikeThresholdType = cp.SpikeThresholdType,
                    SpikeThresholdAdu = cp.SpikeThresholdAdu,
                    SpikeThresholdPercentageRedZone = cp.SpikeThresholdPercentageRedZone,

                    CenterCode = cp.Center.Code,
                    CenterDescription = cp.Center.Description,

                    ProductReference = cp.Product.Reference,
                    ProductDescription = cp.Product.Description,
                    ProductAuxiliarMaterialCode = cp.Product.AuxiliarMaterialCode,
                    ProductUnitOfMeasure = cp.Product.UnitOfMeasure,
                    ProductWeight = cp.Product.Weight,
                    ProductVolume = cp.Product.Volume,
                    ProductBarcode = cp.Product.Barcode,
                    ProductCategory = cp.Product.Category,
                    ProductSegment = cp.Product.Segment,
                    ProductValue = cp.Product.Value,
                    ProductPallet = cp.Product.Pallet,
                    ProductLine = cp.Product.Line,
                    ProductSubline = cp.Product.Subline,
                    ProductBrand = cp.Product.Brand,
                    ProductWorkCenter = cp.Product.WorkCenter,

                    ProviderCode = cp.Provider != null && cp.Provider.deletedAt == null ? cp.Provider.Code : null,
                    ProviderDescription = cp.Provider != null && cp.Provider.deletedAt == null ? cp.Provider.Description : null,

                    BufferProfileName = cp.BufferProfile != null && cp.BufferProfile.deletedAt == null ? cp.BufferProfile.ProfileName : null,
                    TagName = cp.Tag != null && cp.Tag.deletedAt == null ? cp.Tag.Name : null,
                    ReasonName = cp.Reason != null && cp.Reason.deletedAt == null ? cp.Reason.Name : null,
                    AllocationGroupName = cp.AllocationGroup != null && cp.AllocationGroup.deletedAt == null ? cp.AllocationGroup.Name : null,

                    Inbounds = _context.Order
                        .Where(o => o.deletedAt == null && o.IsInbound && o.IdDestinyCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && !o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    FictionalInbounds = _context.Order
                        .Where(o => o.deletedAt == null && o.IsInbound && o.IdDestinyCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    Outbounds = _context.Order
                        .Where(o => o.deletedAt == null && o.IsOutbound && o.IdOriginCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && !o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    FictionalOutbounds = _context.Order
                        .Where(o => o.deletedAt == null && o.IsOutbound && o.IdOriginCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                row.Netflow = UtilsDdmrp.CalculateNetflow(row.Stock, row.QualifiedDemand ?? 0, row.Inbounds);
                row.OrderQuantity = UtilsDdmrp.CalculateOrderQuantity(row.Netflow, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0);
                row.OptimizedOrderQuantity = UtilsDdmrp.CalculateOptimizedOrderQuantity(row.Netflow, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0, row.Moq, row.PackQuantity);
                row.NetflowBufferPercentage = UtilsDdmrp.CalculateBufferPercentage(row.TopOfGreen ?? 0, row.Netflow);
                row.NetflowBufferColor = UtilsDdmrp.CalculateBufferColor(row.Netflow, row.TopOfRed ?? 0, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0);
                row.CoverageDays = UtilsDdmrp.CalculateCoverageDays(row.Stock, row.Adu ?? 0);
                row.ExecutionBufferPercentage = UtilsDdmrp.CalculateBufferPercentage(row.GreenZoneExecution ?? 0, row.Stock);
                row.ExecutionBufferColor = UtilsDdmrp.CalculateBufferColor(row.Stock, row.RedZoneExecution ?? 0, row.YellowZoneExecution ?? 0, row.GreenZoneExecution ?? 0);
            }

            return rows;
        }
    }
}
