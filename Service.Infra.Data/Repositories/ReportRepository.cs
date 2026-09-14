using Microsoft.EntityFrameworkCore;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;
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
            return await _context.CenterProduct
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

                    Entradas = _context.Order
                        .Where(o => o.deletedAt == null && o.IsInbound && o.IdDestinyCenter == cp.IdCenter && o.IdProduct == cp.IdProduct)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    EntradasFicticias = _context.Order
                        .Where(o => o.deletedAt == null && o.IsInbound && o.IdDestinyCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    Saidas = _context.Order
                        .Where(o => o.deletedAt == null && o.IsOutbound && o.IdOriginCenter == cp.IdCenter && o.IdProduct == cp.IdProduct)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    SaidasFicticias = _context.Order
                        .Where(o => o.deletedAt == null && o.IsOutbound && o.IdOriginCenter == cp.IdCenter && o.IdProduct == cp.IdProduct && o.IsFictional)
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0
                })
                .ToListAsync(cancellationToken);
        }
    }
}
