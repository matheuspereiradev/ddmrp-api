using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;
using Service.Domain.Utils;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReportRepository(ApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // Kept fully IQueryable (no ToListAsync, no UtilsDdmrp calls) so OData's [EnableQuery] can compose
        // $filter/$orderby/$top/$skip on top and EF Core translates the whole thing into one SQL query.
        // The fields that UtilsDdmrp computes (Netflow, OrderQuantity, NetflowBufferColor, etc.) are duplicated
        // here as inline ternary/arithmetic expressions the SQL Server provider can translate to CASE WHEN —
        // keep them in sync with UtilsDdmrp (Service.Domain/Utils/UtilsDdmrp.cs) and Formulas.md; parity is
        // covered by ReportRepositoryTests' GetInventoryBufferManagementQueryable_MatchesUtilsDdmrp_* tests.
        //
        // IMPORTANT: never reference CenterProduct's Ignore()'d computed C# properties (TopOfRed/TopOfYellow/
        // TopOfGreen/RedZoneExecution/etc.) from inside this query — e.g. `cp.TopOfGreen`. EF Core can inline
        // a get-only computed property the FIRST time it's translated inside a plain Select, but when OData's
        // [EnableQuery] composes an extra `.Where(row => row.NetflowBufferColor == ...)` on top of this
        // IQueryable, EF Core has to re-derive that property's whole expression a second time to build the
        // WHERE clause — and that re-derivation fails ("Translation of member 'TopOfGreen' ... failed... This
        // commonly occurs when the specified member is unmapped"), even though the exact same formula works
        // fine as a plain Select (confirmed 2026-09-15: reproduced with an InMemory-provider test composing
        // `.Where(r => r.NetflowBufferColor == BufferColor.Red)` on top of this method's result, and with a
        // real SQL Server 500 on `$filter=netflowBufferColor eq 'Red'`). The fix is to compute every zone-top/
        // execution-zone/analytical field here from CenterProduct's *physical* columns (RedZoneBase/RedZoneSafe/
        // YellowZone/GreenZone) directly, thread them through the `.Select()` chain as plain named values, and
        // never touch `cp.TopOfRed`/`cp.RedZoneExecution`/etc. again after this file. Confirmed working (the
        // parity theory below still passes, and a `.Where()` composed on top of the resulting IQueryable no
        // longer throws) — this is *not* a "materialize instead" workaround, it removes the property
        // indirection that was the actual problem.
        public IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable()
        {
            var currentUserId = _currentUser.UserId;

            var withOrderTotals = _context.CenterProduct
                .Where(cp => cp.deletedAt == null && cp.Center.deletedAt == null && cp.Product.deletedAt == null)
                .Select(cp => new
                {
                    Cp = cp,
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
                        .Sum(o => (decimal?)(o.Quantity - o.DeliveredQuantity)) ?? 0,
                    // Left join to the current user's Workspace row for this CenterProduct (one per user+product+center).
                    WorkspaceOptimizedQuantity = _context.Workspace
                        .Where(w => w.deletedAt == null && w.IdCenter == cp.IdCenter && w.IdProduct == cp.IdProduct && w.IdUser == currentUserId)
                        .Select(w => (decimal?)w.OptimizedQuantity)
                        .FirstOrDefault(),
                    WorkspaceApproved = _context.Workspace
                        .Where(w => w.deletedAt == null && w.IdCenter == cp.IdCenter && w.IdProduct == cp.IdProduct && w.IdUser == currentUserId)
                        .Select(w => (bool?)w.Approved)
                        .FirstOrDefault()
                });

            // Zone tops, from CenterProduct.RedZoneBase/RedZoneSafe/YellowZone/GreenZone directly (not cp.TopOfRed/etc.) —
            // same formulas as CenterProduct.TopOfRed/TopOfYellow/TopOfGreen/RedZone.
            var withZoneTops = withOrderTotals.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                TopOfRed = x.Cp.RedZoneBase.HasValue && x.Cp.RedZoneSafe.HasValue
                    ? (decimal?)Math.Ceiling(x.Cp.RedZoneBase.Value + x.Cp.RedZoneSafe.Value)
                    : null,
                RedZone = x.Cp.RedZoneBase.HasValue && x.Cp.RedZoneSafe.HasValue
                    ? (decimal?)Math.Ceiling(x.Cp.RedZoneBase.Value + x.Cp.RedZoneSafe.Value)
                    : null,
                TopOfYellow = x.Cp.RedZoneBase.HasValue && x.Cp.RedZoneSafe.HasValue && x.Cp.YellowZone.HasValue
                    ? (decimal?)Math.Ceiling(x.Cp.RedZoneBase.Value + x.Cp.RedZoneSafe.Value + x.Cp.YellowZone.Value)
                    : null,
                TopOfGreen = x.Cp.RedZoneBase.HasValue && x.Cp.RedZoneSafe.HasValue && x.Cp.YellowZone.HasValue && x.Cp.GreenZone.HasValue
                    ? (decimal?)Math.Ceiling(x.Cp.RedZoneBase.Value + x.Cp.RedZoneSafe.Value + x.Cp.YellowZone.Value + x.Cp.GreenZone.Value)
                    : null,
                GreenZoneExecution = x.Cp.YellowZone.HasValue ? (decimal?)Math.Ceiling(x.Cp.YellowZone.Value) : null
            });

            // Execution zones + analytical zones, from withZoneTops' TopOfRed/RedZone (not cp.RedZoneExecution/etc.) —
            // same formulas as CenterProduct.RedZoneExecution/YellowZoneExecution/RedSafeAnalytical/etc.
            var withExecutionZones = withZoneTops.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                x.TopOfRed,
                x.RedZone,
                x.TopOfYellow,
                x.TopOfGreen,
                x.GreenZoneExecution,
                RedZoneExecution = x.TopOfRed.HasValue ? (decimal?)Math.Ceiling(x.TopOfRed.Value / 2) : null,
                YellowZoneExecution = x.TopOfRed.HasValue ? (decimal?)Math.Ceiling(x.TopOfRed.Value / 2) : null,
                RedSafeAnalytical = x.RedZone.HasValue ? (decimal?)Math.Ceiling(x.RedZone.Value / 2) : null,
                YellowSafeAnalytical = x.RedZone.HasValue ? (decimal?)Math.Ceiling(x.RedZone.Value) : null,
                GreenAnalytical = x.RedZone.HasValue && x.Cp.GreenZone.HasValue ? (decimal?)Math.Ceiling(x.RedZone.Value + x.Cp.GreenZone.Value) : null,
                YellowExcessAnalytical = x.RedZone.HasValue && x.Cp.YellowZone.HasValue ? (decimal?)Math.Ceiling(x.RedZone.Value + x.Cp.YellowZone.Value) : null,
                RedSafeExcessAnalytical = x.RedZone.HasValue ? (decimal?)Math.Ceiling(x.RedZone.Value / 2) : null
            });

            // Top-of-zone for the execution set, from withExecutionZones' RedZoneExecution/YellowZoneExecution/GreenZoneExecution —
            // same formulas as CenterProduct.TopOfRedExecution/TopOfYellowExecution/TopOfGreenExecution.
            var withExecutionTops = withExecutionZones.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                x.TopOfRed,
                x.RedZone,
                x.TopOfYellow,
                x.TopOfGreen,
                x.RedZoneExecution,
                x.YellowZoneExecution,
                x.GreenZoneExecution,
                x.RedSafeAnalytical,
                x.YellowSafeAnalytical,
                x.GreenAnalytical,
                x.YellowExcessAnalytical,
                x.RedSafeExcessAnalytical,
                TopOfRedExecution = x.RedZoneExecution.HasValue ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value) : null,
                TopOfYellowExecution = x.RedZoneExecution.HasValue && x.YellowZoneExecution.HasValue
                    ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value + x.YellowZoneExecution.Value)
                    : null,
                TopOfGreenExecution = x.RedZoneExecution.HasValue && x.YellowZoneExecution.HasValue && x.GreenZoneExecution.HasValue
                    ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value + x.YellowZoneExecution.Value + x.GreenZoneExecution.Value)
                    : null
            });

            // UtilsDdmrp.CalculateNetflow: stock + inbounds - qualifiedDemand
            var withNetflow = withExecutionTops.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                x.TopOfRed,
                x.RedZone,
                x.TopOfYellow,
                x.TopOfGreen,
                x.RedZoneExecution,
                x.YellowZoneExecution,
                x.GreenZoneExecution,
                x.TopOfRedExecution,
                x.TopOfYellowExecution,
                x.TopOfGreenExecution,
                x.RedSafeAnalytical,
                x.YellowSafeAnalytical,
                x.GreenAnalytical,
                x.YellowExcessAnalytical,
                x.RedSafeExcessAnalytical,
                Netflow = x.Cp.Stock + x.Inbounds - (x.Cp.QualifiedDemand ?? 0)
            });

            // UtilsDdmrp.CalculateOrderQuantity: netflow < topOfYellow ? topOfGreen - netflow : 0
            var withOrderQuantity = withNetflow.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                x.TopOfRed,
                x.RedZone,
                x.TopOfYellow,
                x.TopOfGreen,
                x.RedZoneExecution,
                x.YellowZoneExecution,
                x.GreenZoneExecution,
                x.TopOfRedExecution,
                x.TopOfYellowExecution,
                x.TopOfGreenExecution,
                x.RedSafeAnalytical,
                x.YellowSafeAnalytical,
                x.GreenAnalytical,
                x.YellowExcessAnalytical,
                x.RedSafeExcessAnalytical,
                x.Netflow,
                OrderQuantity = x.Netflow < (x.TopOfYellow ?? 0) ? (x.TopOfGreen ?? 0) - x.Netflow : 0,
                // UtilsDdmrp.CalculateSimulatedNetflow(netflow, approved, workspaceOptimizedQuantity)
                SimulatedNetflow = x.Netflow + (x.WorkspaceApproved == true ? (x.WorkspaceOptimizedQuantity ?? 0) : 0)
            });

            var withDerivedMetrics = withOrderQuantity.Select(x => new
            {
                x.Cp,
                x.Inbounds,
                x.FictionalInbounds,
                x.Outbounds,
                x.FictionalOutbounds,
                x.WorkspaceOptimizedQuantity,
                x.WorkspaceApproved,
                x.TopOfRed,
                x.RedZone,
                x.TopOfYellow,
                x.TopOfGreen,
                x.RedZoneExecution,
                x.YellowZoneExecution,
                x.GreenZoneExecution,
                x.TopOfRedExecution,
                x.TopOfYellowExecution,
                x.TopOfGreenExecution,
                x.RedSafeAnalytical,
                x.YellowSafeAnalytical,
                x.GreenAnalytical,
                x.YellowExcessAnalytical,
                x.RedSafeExcessAnalytical,
                x.Netflow,
                x.OrderQuantity,
                x.SimulatedNetflow,
                // UtilsDdmrp.CalculateOptimizedOrderQuantity — the system-suggested value, before any Workspace override
                SystemOptimizedOrderQuantity = x.Cp.PackQuantity == 0
                    ? 0
                    : (x.OrderQuantity < x.Cp.Moq ? 0 : Math.Floor(x.OrderQuantity / x.Cp.PackQuantity) * x.Cp.PackQuantity),
                // UtilsDdmrp.CalculateBufferPercentage(topOfGreen, delta: netflow)
                NetflowBufferPercentage = (x.TopOfGreen ?? 0) == 0 ? 0 : x.Netflow / (x.TopOfGreen ?? 0),
                // UtilsDdmrp.CalculateBufferColor(quantity: netflow, topOfRed, topOfYellow, topOfGreen)
                NetflowBufferColor = (x.TopOfGreen ?? 0) == 0 ? BufferColor.NoColor
                    : x.Netflow < 0 ? BufferColor.Black
                    : x.Netflow > (x.TopOfGreen ?? 0) ? BufferColor.Blue
                    : x.Netflow <= (x.TopOfRed ?? 0) ? BufferColor.Red
                    : x.Netflow <= (x.TopOfYellow ?? 0) ? BufferColor.Yellow
                    : BufferColor.Green,
                // UtilsDdmrp.CalculateBufferPercentage(topOfGreen, delta: simulatedNetflow)
                SimulatedNetflowBufferPercentage = (x.TopOfGreen ?? 0) == 0 ? 0 : x.SimulatedNetflow / (x.TopOfGreen ?? 0),
                // UtilsDdmrp.CalculateBufferColor(quantity: simulatedNetflow, topOfRed, topOfYellow, topOfGreen)
                SimulatedNetflowBufferColor = (x.TopOfGreen ?? 0) == 0 ? BufferColor.NoColor
                    : x.SimulatedNetflow < 0 ? BufferColor.Black
                    : x.SimulatedNetflow > (x.TopOfGreen ?? 0) ? BufferColor.Blue
                    : x.SimulatedNetflow <= (x.TopOfRed ?? 0) ? BufferColor.Red
                    : x.SimulatedNetflow <= (x.TopOfYellow ?? 0) ? BufferColor.Yellow
                    : BufferColor.Green,
                // UtilsDdmrp.CalculateCoverageDays(availableStock: Stock, adu)
                CoverageDays = (x.Cp.Adu ?? 0) > 0 ? x.Cp.Stock / (x.Cp.Adu ?? 0) : 0,
                // UtilsDdmrp.CalculateBufferPercentage(topOfGreen: greenZoneExecution, delta: Stock)
                ExecutionBufferPercentage = (x.GreenZoneExecution ?? 0) == 0 ? 0 : x.Cp.Stock / (x.GreenZoneExecution ?? 0),
                // UtilsDdmrp.CalculateBufferColor(quantity: Stock, redZoneExecution, yellowZoneExecution, greenZoneExecution)
                ExecutionBufferColor = (x.GreenZoneExecution ?? 0) == 0 ? BufferColor.NoColor
                    : x.Cp.Stock < 0 ? BufferColor.Black
                    : x.Cp.Stock > (x.GreenZoneExecution ?? 0) ? BufferColor.Blue
                    : x.Cp.Stock <= (x.RedZoneExecution ?? 0) ? BufferColor.Red
                    : x.Cp.Stock <= (x.YellowZoneExecution ?? 0) ? BufferColor.Yellow
                    : BufferColor.Green
            });

            return withDerivedMetrics.Select(x => new InventoryBufferManagementRow
            {
                Id = x.Cp.Id,
                IdProduct = x.Cp.IdProduct,
                IdCenter = x.Cp.IdCenter,
                IdOriginCenter = x.Cp.IdOriginCenter,
                PackQuantity = x.Cp.PackQuantity,
                Moq = x.Cp.Moq,
                LeadTime = x.Cp.LeadTime,
                Frequency = x.Cp.Frequency,
                Class = x.Cp.Class,
                Classification = x.Cp.Classification,
                Segment = x.Cp.Segment,
                Stock = x.Cp.Stock,
                IdProvider = x.Cp.IdProvider,
                IdTag = x.Cp.IdTag,
                IdReason = x.Cp.IdReason,
                IdAllocationGroup = x.Cp.IdAllocationGroup,
                IdBufferProfile = x.Cp.IdBufferProfile,
                Adu = x.Cp.Adu,
                FutureAduDays = x.Cp.FutureAduDays,
                HistoryAduDays = x.Cp.HistoryAduDays,
                Adi = x.Cp.Adi,
                StandardDeviation = x.Cp.StandardDeviation,
                Cv = x.Cp.Cv,
                UseSuggestedLTFactor = x.Cp.UseSuggestedLTFactor,
                UseSuggestedVariabilityFactor = x.Cp.UseSuggestedVariabilityFactor,
                RedZoneBase = x.Cp.RedZoneBase,
                RedZoneSafe = x.Cp.RedZoneSafe,
                RedZone = x.RedZone,
                YellowZone = x.Cp.YellowZone,
                GreenZone = x.Cp.GreenZone,
                TopOfRed = x.TopOfRed,
                TopOfYellow = x.TopOfYellow,
                TopOfGreen = x.TopOfGreen,
                RedZoneExecution = x.RedZoneExecution,
                YellowZoneExecution = x.YellowZoneExecution,
                GreenZoneExecution = x.GreenZoneExecution,
                TopOfRedExecution = x.TopOfRedExecution,
                TopOfYellowExecution = x.TopOfYellowExecution,
                TopOfGreenExecution = x.TopOfGreenExecution,
                RedSafeAnalytical = x.RedSafeAnalytical,
                YellowSafeAnalytical = x.YellowSafeAnalytical,
                GreenAnalytical = x.GreenAnalytical,
                YellowExcessAnalytical = x.YellowExcessAnalytical,
                RedSafeExcessAnalytical = x.RedSafeExcessAnalytical,
                UseDafOnGreenZone = x.Cp.UseDafOnGreenZone,
                CustomLeadTimeFactor = x.Cp.CustomLeadTimeFactor,
                CustomVariabilityFactor = x.Cp.CustomVariabilityFactor,
                GreenZoneParametrizationUseMoq = x.Cp.GreenZoneParametrizationUseMoq,
                GreenZoneParametrizationUseAduXFrequency = x.Cp.GreenZoneParametrizationUseAduXFrequency,
                GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = x.Cp.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
                BufferType = x.Cp.BufferType,
                ZafRedZone = x.Cp.ZafRedZone,
                ZafYellowZone = x.Cp.ZafYellowZone,
                ZafGreenZone = x.Cp.ZafGreenZone,
                QualifiedDemand = x.Cp.QualifiedDemand,
                SpikeHorizonType = x.Cp.SpikeHorizonType,
                SpikeHorizonValue = x.Cp.SpikeHorizonValue,
                SpikeHorizonLTDays = x.Cp.SpikeHorizonLTDays,
                SpikeThresholdType = x.Cp.SpikeThresholdType,
                SpikeThresholdAdu = x.Cp.SpikeThresholdAdu,
                SpikeThresholdPercentageRedZone = x.Cp.SpikeThresholdPercentageRedZone,

                CenterCode = x.Cp.Center.Code,
                CenterDescription = x.Cp.Center.Description,

                ProductReference = x.Cp.Product.Reference,
                ProductDescription = x.Cp.Product.Description,
                ProductAuxiliarMaterialCode = x.Cp.Product.AuxiliarMaterialCode,
                ProductUnitOfMeasure = x.Cp.Product.UnitOfMeasure,
                ProductWeight = x.Cp.Product.Weight,
                ProductVolume = x.Cp.Product.Volume,
                ProductBarcode = x.Cp.Product.Barcode,
                ProductCategory = x.Cp.Product.Category,
                ProductSegment = x.Cp.Product.Segment,
                ProductValue = x.Cp.Product.Value,
                ProductPallet = x.Cp.Product.Pallet,
                ProductLine = x.Cp.Product.Line,
                ProductSubline = x.Cp.Product.Subline,
                ProductBrand = x.Cp.Product.Brand,
                ProductWorkCenter = x.Cp.Product.WorkCenter,

                ProviderCode = x.Cp.Provider != null && x.Cp.Provider.deletedAt == null ? x.Cp.Provider.Code : null,
                ProviderDescription = x.Cp.Provider != null && x.Cp.Provider.deletedAt == null ? x.Cp.Provider.Description : null,

                BufferProfileName = x.Cp.BufferProfile != null && x.Cp.BufferProfile.deletedAt == null ? x.Cp.BufferProfile.ProfileName : null,
                TagName = x.Cp.Tag != null && x.Cp.Tag.deletedAt == null ? x.Cp.Tag.Name : null,
                ReasonName = x.Cp.Reason != null && x.Cp.Reason.deletedAt == null ? x.Cp.Reason.Name : null,
                AllocationGroupName = x.Cp.AllocationGroup != null && x.Cp.AllocationGroup.deletedAt == null ? x.Cp.AllocationGroup.Name : null,

                Inbounds = x.Inbounds,
                FictionalInbounds = x.FictionalInbounds,
                Outbounds = x.Outbounds,
                FictionalOutbounds = x.FictionalOutbounds,

                Netflow = x.Netflow,
                OrderQuantity = x.OrderQuantity,
                SystemOptimizedOrderQuantity = x.SystemOptimizedOrderQuantity,
                HasSuggestion = x.SystemOptimizedOrderQuantity > 0,
                OptimizedOrderQuantity = x.WorkspaceOptimizedQuantity ?? x.SystemOptimizedOrderQuantity,
                Approved = x.WorkspaceApproved ?? false,
                NetflowBufferPercentage = x.NetflowBufferPercentage,
                SimulatedNetflowBufferPercentage = x.SimulatedNetflowBufferPercentage,
                NetflowBufferColor = x.NetflowBufferColor,
                SimulatedNetflowBufferColor = x.SimulatedNetflowBufferColor,
                CoverageDays = x.CoverageDays,
                ExecutionBufferPercentage = x.ExecutionBufferPercentage,
                ExecutionBufferColor = x.ExecutionBufferColor
            });
        }

        // Reuses GetInventoryBufferManagementQueryable() so the Workspace-scoped SimulatedNetflow* fields
        // (left-joined to the current user's Workspace row inside that method) stay a single source of truth —
        // used by WorkspaceService to return the post-update simulated buffer for one CenterProduct.
        public async Task<InventoryBufferManagementRow?> GetInventoryBufferManagementRowAsync(int idCenter, int idProduct, CancellationToken cancellationToken = default)
        {
            return await GetInventoryBufferManagementQueryable()
                .Where(r => r.IdCenter == idCenter && r.IdProduct == idProduct)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default)
        {
            var query = _context.Order
                .Where(o => o.deletedAt == null && o.Quantity > o.DeliveredQuantity && o.IsInbound && !o.IsFictional);

            if (idCenter.HasValue)
                query = query.Where(o => o.IdDestinyCenter == idCenter.Value);

            if (idProduct.HasValue)
                query = query.Where(o => o.IdProduct == idProduct.Value);

            var rows = await query
                .Select(o => new OpenOrderRow
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,

                    IdPartner = o.IdPartner,
                    PartnerCode = o.Partner != null && o.Partner.deletedAt == null ? o.Partner.Code : null,
                    PartnerDescription = o.Partner != null && o.Partner.deletedAt == null ? o.Partner.Description : null,

                    IdDestinyCenter = o.IdDestinyCenter,
                    DestinyCenterCode = o.DestinyCenter != null && o.DestinyCenter.deletedAt == null ? o.DestinyCenter.Code : null,
                    DestinyCenterDescription = o.DestinyCenter != null && o.DestinyCenter.deletedAt == null ? o.DestinyCenter.Description : null,

                    IdOriginCenter = o.IdOriginCenter,
                    OriginCenterCode = o.OriginCenter != null && o.OriginCenter.deletedAt == null ? o.OriginCenter.Code : null,
                    OriginCenterDescription = o.OriginCenter != null && o.OriginCenter.deletedAt == null ? o.OriginCenter.Description : null,

                    IdProduct = o.IdProduct,
                    ProductReference = o.Product.Reference,
                    ProductDescription = o.Product.Description,

                    Quantity = o.Quantity,
                    DeliveredQuantity = o.DeliveredQuantity,
                    PendingQuantity = o.PendingQuantity,
                    MeasurementUnit = o.MeasurementUnit,
                    Position = o.Position,
                    CreationDate = o.CreationDate,
                    DeliveryDate = o.DeliveryDate,
                    OrderLeadtime = o.OrderLeadtime,
                    Notes = o.Notes,
                    Type = o.Type,
                    IsInbound = o.IsInbound,
                    IsOutbound = o.IsOutbound,
                    IsFictional = o.IsFictional
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                if (row.DeliveryDate.HasValue && row.OrderLeadtime.HasValue)
                {
                    row.TimeBuffer = UtilsDdmrp.CalculateTimeBuffer(row.DeliveryDate.Value, row.OrderLeadtime.Value);
                    row.TimeBufferColor = UtilsDdmrp.CalculateTimeBufferColor(row.TimeBuffer.Value);
                }

                row.DaysToReceive = UtilsDdmrp.CalculateDaysToReceive(row.DeliveryDate);
                row.DaysLate = UtilsDdmrp.CalculateDaysLate(row.DeliveryDate);
            }

            await ApplyExecutionBufferAsync(rows, cancellationToken);

            return rows;
        }

        private async Task ApplyExecutionBufferAsync(List<OpenOrderRow> rows, CancellationToken cancellationToken)
        {
            var idProducts = rows.Select(r => r.IdProduct).Distinct().ToList();
            var idCenters = rows.Where(r => r.IdDestinyCenter.HasValue).Select(r => r.IdDestinyCenter!.Value).Distinct().ToList();

            if (idProducts.Count == 0 || idCenters.Count == 0)
                return;

            var referenceOrders = await _context.Order
                .Where(o => o.deletedAt == null && o.Quantity > o.DeliveredQuantity && o.IsInbound && !o.IsFictional
                    && o.IdDestinyCenter.HasValue && idProducts.Contains(o.IdProduct) && idCenters.Contains(o.IdDestinyCenter!.Value))
                .Select(o => new { o.Id, o.IdProduct, o.IdDestinyCenter, o.DeliveryDate, o.PendingQuantity })
                .ToListAsync(cancellationToken);

            var centerProducts = await _context.CenterProduct
                .Where(cp => cp.deletedAt == null && idProducts.Contains(cp.IdProduct) && idCenters.Contains(cp.IdCenter))
                .Select(cp => new { cp.IdProduct, cp.IdCenter, cp.Stock, cp.TopOfRedExecution, cp.TopOfYellowExecution, cp.TopOfGreenExecution })
                .ToListAsync(cancellationToken);

            var centerProductLookup = centerProducts.ToDictionary(cp => (cp.IdProduct, cp.IdCenter));

            foreach (var row in rows)
            {
                if (!row.IdDestinyCenter.HasValue || !row.DeliveryDate.HasValue)
                    continue;

                if (!centerProductLookup.TryGetValue((row.IdProduct, row.IdDestinyCenter.Value), out var centerProduct))
                    continue;

                if (!centerProduct.TopOfYellowExecution.HasValue || centerProduct.TopOfYellowExecution.Value == 0)
                    continue;

                var pendingFromEarlierOrders = referenceOrders
                    .Where(o => o.IdProduct == row.IdProduct && o.IdDestinyCenter == row.IdDestinyCenter
                        && o.Id < row.Id && o.DeliveryDate.HasValue && o.DeliveryDate.Value <= row.DeliveryDate.Value)
                    .Sum(o => o.PendingQuantity);

                var quantity = centerProduct.Stock + pendingFromEarlierOrders;
                row.ExecutionBuffer = quantity / centerProduct.TopOfYellowExecution.Value;
                row.ExecutionBufferColor = UtilsDdmrp.CalculateBufferColor(
                    quantity,
                    centerProduct.TopOfRedExecution ?? 0,
                    centerProduct.TopOfYellowExecution.Value,
                    centerProduct.TopOfGreenExecution ?? 0);
            }
        }
    }
}
