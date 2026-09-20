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
                // Matches CenterProduct.GreenAnalytical (GreenZone alone, not RedZone + GreenZone) — fixed
                // 2026-09-20, this used to double-count RedZone here, diverging from the entity's own formula.
                GreenAnalytical = x.Cp.GreenZone.HasValue ? (decimal?)Math.Ceiling(x.Cp.GreenZone.Value) : null,
                // UtilsDdmrp: 0 when (RedZone + GreenZone) >= (RedZone + YellowZone), else CEILING((RedZone + YellowZone) - (RedZone + GreenZone))
                YellowExcessAnalytical = x.RedZone.HasValue && x.Cp.YellowZone.HasValue && x.Cp.GreenZone.HasValue
                    ? ((x.RedZone.Value + x.Cp.GreenZone.Value) >= (x.RedZone.Value + x.Cp.YellowZone.Value)
                        ? (decimal?)0m
                        : (decimal?)Math.Ceiling((x.RedZone.Value + x.Cp.YellowZone.Value) - (x.RedZone.Value + x.Cp.GreenZone.Value)))
                    : null
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
                // UtilsDdmrp: 0 when TopOfGreen <= 0, else CEILING(TopOfGreen - (RedZone + GreenZone + YellowExcessAnalytical))
                RedExcessAnalytical = x.TopOfGreen.HasValue && x.RedZone.HasValue && x.Cp.GreenZone.HasValue && x.YellowExcessAnalytical.HasValue
                    ? (x.TopOfGreen.Value <= 0
                        ? (decimal?)0m
                        : (decimal?)Math.Ceiling(x.TopOfGreen.Value - (x.RedZone.Value + x.Cp.GreenZone.Value + x.YellowExcessAnalytical.Value)))
                    : null,
                TopOfRedExecution = x.RedZoneExecution.HasValue ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value) : null,
                TopOfYellowExecution = x.RedZoneExecution.HasValue && x.YellowZoneExecution.HasValue
                    ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value + x.YellowZoneExecution.Value)
                    : null,
                TopOfGreenExecution = x.RedZoneExecution.HasValue && x.YellowZoneExecution.HasValue && x.GreenZoneExecution.HasValue
                    ? (decimal?)Math.Ceiling(x.RedZoneExecution.Value + x.YellowZoneExecution.Value + x.GreenZoneExecution.Value)
                    : null
            });

            // UtilsDdmrp.CalculateNetflow: availableStock (stock - reservedStock) + inbounds - qualifiedDemand
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
                x.RedExcessAnalytical,
                Netflow = (x.Cp.Stock - x.Cp.ReservedStock) + x.Inbounds - (x.Cp.QualifiedDemand ?? 0)
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
                x.RedExcessAnalytical,
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
                x.RedExcessAnalytical,
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
                    : x.Netflow <= 0 ? BufferColor.Black
                    : x.Netflow > (x.TopOfGreen ?? 0) ? BufferColor.Blue
                    : x.Netflow <= (x.TopOfRed ?? 0) ? BufferColor.Red
                    : x.Netflow <= (x.TopOfYellow ?? 0) ? BufferColor.Yellow
                    : BufferColor.Green,
                // UtilsDdmrp.CalculateBufferPercentage(topOfGreen, delta: simulatedNetflow)
                SimulatedNetflowBufferPercentage = (x.TopOfGreen ?? 0) == 0 ? 0 : x.SimulatedNetflow / (x.TopOfGreen ?? 0),
                // UtilsDdmrp.CalculateBufferColor(quantity: simulatedNetflow, topOfRed, topOfYellow, topOfGreen)
                SimulatedNetflowBufferColor = (x.TopOfGreen ?? 0) == 0 ? BufferColor.NoColor
                    : x.SimulatedNetflow <= 0 ? BufferColor.Black
                    : x.SimulatedNetflow > (x.TopOfGreen ?? 0) ? BufferColor.Blue
                    : x.SimulatedNetflow <= (x.TopOfRed ?? 0) ? BufferColor.Red
                    : x.SimulatedNetflow <= (x.TopOfYellow ?? 0) ? BufferColor.Yellow
                    : BufferColor.Green,
                // UtilsDdmrp.CalculateCoverageDays(availableStock: Stock - ReservedStock, adu)
                CoverageDays = (x.Cp.Adu ?? 0) > 0 ? (x.Cp.Stock - x.Cp.ReservedStock) / (x.Cp.Adu ?? 0) : 0,
                // UtilsDdmrp.CalculateBufferPercentage(topOfGreen: topOfYellowExecution, delta: Stock)
                ExecutionBufferPercentage = (x.TopOfYellowExecution ?? 0) == 0 ? 0 : x.Cp.Stock / (x.TopOfYellowExecution ?? 0),
                // UtilsDdmrp.CalculateBufferColor(quantity: Stock, topOfRedExecution, topOfYellowExecution, topOfGreenExecution)
                ExecutionBufferColor = (x.TopOfGreenExecution ?? 0) == 0 ? BufferColor.NoColor
                    : x.Cp.Stock <= 0 ? BufferColor.Black
                    : x.Cp.Stock > (x.TopOfGreenExecution ?? 0) ? BufferColor.Blue
                    : x.Cp.Stock <= (x.TopOfRedExecution ?? 0) ? BufferColor.Red
                    : x.Cp.Stock <= (x.TopOfYellowExecution ?? 0) ? BufferColor.Yellow
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
                ReservedStock = x.Cp.ReservedStock,
                AvailableStock = x.Cp.Stock - x.Cp.ReservedStock,
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
                FixedBufferProfile = x.Cp.FixedBufferProfile,
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
                RedExcessAnalytical = x.RedExcessAnalytical,
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

        // Feeds AllocationGroupService's efficient-distribution ("DE") algorithm: only the current user's
        // approved Workspace items for the given group, with Netflow/TopOfGreen/Moq/PackQuantity already
        // computed by the shared inventory-buffer queryable, so that math stays a single source of truth.
        public async Task<List<InventoryBufferManagementRow>> GetApprovedByAllocationGroupAsync(int idAllocationGroup, CancellationToken cancellationToken = default)
        {
            return await GetInventoryBufferManagementQueryable()
                .Where(r => r.IdAllocationGroup == idAllocationGroup && r.Approved)
                .ToListAsync(cancellationToken);
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

        // History rows for the requested period (robot-written daily snapshot columns, see CLAUDE.md/Formulas.md)
        // unioned with one "today" row built live from CenterProduct + Order — History only gets a row once the
        // Robot has run for that day, so "today" (before that day's run) has no History row yet and is filled
        // in from the current CenterProduct state instead.
        public async Task<List<InventoryHistoryRow>> GetInventoryHistoryAsync(int idCenter, int idProduct, DateTime dateStart, DateTime dateEnd, CancellationToken cancellationToken = default)
        {
            var rows = await _context.History
                .Where(h => h.deletedAt == null && h.IdCenter == idCenter && h.IdProduct == idProduct && h.Date >= dateStart && h.Date <= dateEnd)
                .Select(h => new InventoryHistoryRow
                {
                    Date = h.Date,
                    Stock = h.Stock,
                    QualifiedDemand = h.QualifiedDemand,
                    OrdersInTransit = h.OpenInbounds,
                    Consumption = h.Consumption,
                    StockTotal = h.Stock.HasValue && h.OpenInbounds.HasValue ? h.Stock.Value + h.OpenInbounds.Value : (decimal?)null,
                    Adu = h.Adu,
                    RedSafeZone = h.RedSafeZone,
                    RedBaseZone = h.RedBaseZone,
                    RedZone = h.RedSafeZone.HasValue && h.RedBaseZone.HasValue ? h.RedSafeZone.Value + h.RedBaseZone.Value : (decimal?)null,
                    YellowZone = h.YellowZone,
                    GreenZone = h.GreenZone,
                    InventoryDays = h.Stock.HasValue && h.Adu.HasValue && h.Adu.Value != 0 ? h.Stock.Value / h.Adu.Value : (decimal?)null
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
                row.Netflow = UtilsDdmrp.CalculateNetflow(row.Stock ?? 0, row.QualifiedDemand ?? 0, row.OrdersInTransit ?? 0);

            var centerProduct = await _context.CenterProduct
                .Where(cp => cp.deletedAt == null && cp.IdCenter == idCenter && cp.IdProduct == idProduct)
                .Select(cp => new { cp.Stock, cp.QualifiedDemand, cp.Adu, cp.RedZoneBase, cp.RedZoneSafe, cp.YellowZone, cp.GreenZone })
                .FirstOrDefaultAsync(cancellationToken);

            if (centerProduct != null)
            {
                var ordersInTransit = await _context.Order
                    .Where(o => o.deletedAt == null && o.IsInbound && !o.IsFictional && o.IdDestinyCenter == idCenter && o.IdProduct == idProduct)
                    .SumAsync(o => (decimal?)(o.Quantity - o.DeliveredQuantity), cancellationToken) ?? 0;

                var outbounds = await _context.Order
                    .Where(o => o.deletedAt == null && o.IsOutbound && !o.IsFictional && o.IdOriginCenter == idCenter && o.IdProduct == idProduct)
                    .SumAsync(o => (decimal?)(o.Quantity - o.DeliveredQuantity), cancellationToken) ?? 0;

                var stock = centerProduct.Stock;
                var stockTotal = stock + ordersInTransit;

                rows.Add(new InventoryHistoryRow
                {
                    Date = DateTime.Today,
                    Stock = stock,
                    QualifiedDemand = centerProduct.QualifiedDemand,
                    OrdersInTransit = ordersInTransit,
                    Consumption = Math.Max(outbounds, centerProduct.Adu ?? 0),
                    StockTotal = stockTotal,
                    Adu = centerProduct.Adu,
                    RedSafeZone = centerProduct.RedZoneSafe,
                    RedBaseZone = centerProduct.RedZoneBase,
                    RedZone = centerProduct.RedZoneSafe.HasValue && centerProduct.RedZoneBase.HasValue
                        ? centerProduct.RedZoneSafe.Value + centerProduct.RedZoneBase.Value
                        : (decimal?)null,
                    YellowZone = centerProduct.YellowZone,
                    GreenZone = centerProduct.GreenZone,
                    InventoryDays = centerProduct.Adu.HasValue && centerProduct.Adu.Value != 0 ? stock / centerProduct.Adu.Value : null,
                    Netflow = UtilsDdmrp.CalculateNetflow(stock, centerProduct.QualifiedDemand ?? 0, ordersInTransit)
                });
            }

            return rows.OrderBy(r => r.Date).ToList();
        }

        // Simulates future stock day by day for a single (IdProduct, IdCenter) pair. Per-day inputs (the
        // Forecast daily breakdown, split across business days exactly like ForecastRepository.GetFilteredAsync;
        // pending inbound/outbound Orders grouped by DeliveryDate, fictional orders included unless
        // useFictionalOrders is false) are pulled with regular LINQ/GroupBy, bounded by the requested date
        // range — but the carry-forward itself (each day's OpeningStock is the previous day's ClosingStock)
        // is inherently sequential and stateful, so it can't be expressed as one SQL query; it runs as a
        // plain C# loop over the already-materialized per-day data.
        public async Task<List<ProjectedStockAlertRow>> GetProjectedStockAlertAsync(
            int idCenter,
            int idProduct,
            DateTime dateStart,
            DateTime dateEnd,
            bool useAdu = true,
            bool useForecast = true,
            bool useInbounds = true,
            bool useOutbounds = true,
            bool accumulateInboundsToday = false,
            bool accumulateOutboundsToday = false,
            bool useFictionalOrders = true,
            CancellationToken cancellationToken = default)
        {
            var centerProduct = await _context.CenterProduct
                .Where(cp => cp.deletedAt == null && cp.IdCenter == idCenter && cp.IdProduct == idProduct)
                .Select(cp => new
                {
                    cp.Adu,
                    cp.Stock,
                    cp.RedZoneExecution,
                    cp.YellowZoneExecution,
                    cp.GreenZoneExecution,
                    cp.TopOfRedExecution,
                    cp.TopOfYellowExecution,
                    cp.TopOfGreenExecution
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (centerProduct == null)
                return new List<ProjectedStockAlertRow>();

            var productReference = await _context.Product
                .Where(p => p.Id == idProduct)
                .Select(p => p.Reference)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            var centerCode = await _context.Center
                .Where(c => c.Id == idCenter)
                .Select(c => c.Code)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            var dates = await _context.Calendar
                .Where(c => c.Date >= dateStart && c.Date <= dateEnd)
                .OrderBy(c => c.Date)
                .Select(c => c.Date)
                .ToListAsync(cancellationToken);

            var forecasts = _context.Forecast.Where(f => f.deletedAt == null && f.IdProduct == idProduct && f.IdCenter == idCenter);
            var workingDays = _context.Calendar.Where(c => c.IsWorkingDay);

            var withBusinessDayCount = forecasts.Select(f => new
            {
                f.StartDate,
                f.EndDate,
                f.Value,
                BusinessDayCount = workingDays.Count(c => c.Date >= f.StartDate && c.Date <= f.EndDate)
            });

            var forecastByDate = await (
                from f in withBusinessDayCount
                from d in _context.Calendar
                where d.Date >= f.StartDate && d.Date <= f.EndDate && d.Date >= dateStart && d.Date <= dateEnd
                select new
                {
                    d.Date,
                    Value = d.IsWorkingDay && f.BusinessDayCount > 0 ? f.Value / f.BusinessDayCount : 0
                })
                .GroupBy(x => x.Date)
                .Select(g => new { Date = g.Key, Value = g.Sum(x => x.Value) })
                .ToDictionaryAsync(x => x.Date, x => x.Value, cancellationToken);

            var today = DateTime.Today;

            var inboundByDate = await _context.Order
                .Where(o => o.deletedAt == null && o.IsInbound && (useFictionalOrders || !o.IsFictional) && o.IdProduct == idProduct
                    && o.IdDestinyCenter == idCenter && o.DeliveryDate.HasValue)
                .Select(o => new
                {
                    EffectiveDate = accumulateInboundsToday && o.DeliveryDate!.Value < today ? today : o.DeliveryDate!.Value,
                    Pending = o.Quantity - o.DeliveredQuantity
                })
                .Where(x => x.EffectiveDate >= dateStart && x.EffectiveDate <= dateEnd)
                .GroupBy(x => x.EffectiveDate)
                .Select(g => new { Date = g.Key, Value = g.Sum(x => x.Pending) })
                .ToDictionaryAsync(x => x.Date, x => x.Value, cancellationToken);

            var outboundByDate = await _context.Order
                .Where(o => o.deletedAt == null && o.IsOutbound && (useFictionalOrders || !o.IsFictional) && o.IdProduct == idProduct
                    && o.IdOriginCenter == idCenter && o.DeliveryDate.HasValue)
                .Select(o => new
                {
                    EffectiveDate = accumulateOutboundsToday && o.DeliveryDate!.Value < today ? today : o.DeliveryDate!.Value,
                    Pending = o.Quantity - o.DeliveredQuantity
                })
                .Where(x => x.EffectiveDate >= dateStart && x.EffectiveDate <= dateEnd)
                .GroupBy(x => x.EffectiveDate)
                .Select(g => new { Date = g.Key, Value = g.Sum(x => x.Pending) })
                .ToDictionaryAsync(x => x.Date, x => x.Value, cancellationToken);

            var adu = centerProduct.Adu ?? 0;
            var redZoneExecution = centerProduct.RedZoneExecution ?? 0;
            var yellowZoneExecution = centerProduct.YellowZoneExecution ?? 0;
            var greenZoneExecution = centerProduct.GreenZoneExecution ?? 0;
            var topOfRedExecution = centerProduct.TopOfRedExecution ?? 0;
            var topOfYellowExecution = centerProduct.TopOfYellowExecution ?? 0;
            var topOfGreenExecution = centerProduct.TopOfGreenExecution ?? 0;

            var rows = new List<ProjectedStockAlertRow>(dates.Count);
            var stock = centerProduct.Stock;

            foreach (var date in dates)
            {
                var projectedConsumption = forecastByDate.GetValueOrDefault(date, 0);
                var inbound = inboundByDate.GetValueOrDefault(date, 0);
                var outboundOrders = outboundByDate.GetValueOrDefault(date, 0);

                var outboundCandidates = new List<decimal>();
                if (useAdu) outboundCandidates.Add(adu);
                if (useOutbounds) outboundCandidates.Add(outboundOrders);
                if (useForecast) outboundCandidates.Add(projectedConsumption);
                var outbound = outboundCandidates.Count > 0 ? outboundCandidates.Max() : 0m;

                var openingStock = stock;
                var closingStock = openingStock - outbound + (useInbounds ? inbound : 0);

                rows.Add(new ProjectedStockAlertRow
                {
                    Date = date,
                    IdProduct = idProduct,
                    IdCenter = idCenter,
                    ProductReference = productReference,
                    CenterCode = centerCode,
                    Adu = adu,
                    RedZoneExecution = redZoneExecution,
                    YellowZoneExecution = yellowZoneExecution,
                    GreenZoneExecution = greenZoneExecution,
                    ProjectedConsumption = projectedConsumption,
                    Inbound = inbound,
                    OutboundOrders = outboundOrders,
                    Outbound = outbound,
                    OpeningStock = openingStock,
                    ClosingStock = closingStock,
                    ExecutionBufferColor = UtilsDdmrp.CalculateBufferColor(closingStock, topOfRedExecution, topOfYellowExecution, topOfGreenExecution)
                });

                stock = closingStock;
            }

            return rows;
        }

        // One row per (IdProduct, IdCenter) pair with History rows in the requested period, counting how many
        // days each day's buffer color (UtilsDdmrp.CalculateBufferColor) landed in. Zones/Stock/QualifiedDemand/
        // OpenInbounds null on a History row (no zone calculated yet that day) are treated as 0, which
        // CalculateBufferColor already resolves to NoColor via its topOfGreen == 0 guard. History rows are
        // inner-joined to a non-deleted Product/Center, same convention as InventoryBufferManagementRow.
        //
        // mode picks which quantity/zone set feeds CalculateBufferColor per day, mirroring the Netflow vs.
        // Execution buffer distinction already used elsewhere (InventoryBufferManagementRow's
        // NetflowBufferColor/ExecutionBufferColor, OpenOrderRow.ExecutionBufferColor):
        //   Netflow   -> quantity = Netflow(Stock, QualifiedDemand, OpenInbounds), zones = RedZoneBase+RedZoneSafe/YellowZone/GreenZone
        //   Execution -> quantity = Stock only (no Netflow), zones = RedZoneExecution/YellowZoneExecution/GreenZoneExecution
        //                (same formulas as CenterProduct.RedZoneExecution/YellowZoneExecution/GreenZoneExecution:
        //                RedZoneExecution = YellowZoneExecution = TopOfRed / 2, GreenZoneExecution = YellowZone)
        public async Task<List<BufferPenetrationRow>> GetBufferPenetrationAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            BufferPenetrationMode mode = BufferPenetrationMode.Netflow,
            CancellationToken cancellationToken = default)
        {
            var query = _context.History
                .Where(h => h.deletedAt == null
                    && h.Date >= dateStart && h.Date <= dateEnd
                    && h.Product.deletedAt == null
                    && h.Center.deletedAt == null);

            if (idCenters != null && idCenters.Length > 0)
                query = query.Where(h => idCenters.Contains(h.IdCenter));

            if (idProduct.HasValue)
                query = query.Where(h => h.IdProduct == idProduct.Value);

            var historyRows = await query
                .Select(h => new
                {
                    h.IdProduct,
                    ProductReference = h.Product.Reference,
                    ProductDescription = h.Product.Description,
                    h.IdCenter,
                    CenterCode = h.Center.Code,
                    h.Stock,
                    h.QualifiedDemand,
                    h.OpenInbounds,
                    h.RedBaseZone,
                    h.RedSafeZone,
                    h.YellowZone,
                    h.GreenZone
                })
                .ToListAsync(cancellationToken);

            var rows = historyRows
                .GroupBy(h => (h.IdProduct, h.ProductReference, h.ProductDescription, h.IdCenter, h.CenterCode))
                .Select(group =>
                {
                    var row = new BufferPenetrationRow
                    {
                        IdProduct = group.Key.IdProduct,
                        ReferenceProduct = group.Key.ProductReference,
                        DescriptionProduct = group.Key.ProductDescription,
                        IdCenter = group.Key.IdCenter,
                        CenterCode = group.Key.CenterCode,
                        QuantityDays = group.Count()
                    };

                    foreach (var h in group)
                    {
                        var (netflowColor, executionColor) = ComputeBufferColors(
                            h.Stock, h.QualifiedDemand, h.OpenInbounds, h.RedBaseZone, h.RedSafeZone, h.YellowZone, h.GreenZone);
                        var color = mode == BufferPenetrationMode.Execution ? executionColor : netflowColor;

                        switch (color)
                        {
                            case BufferColor.Black: row.DaysBlack++; break;
                            case BufferColor.Red: row.DaysRed++; break;
                            case BufferColor.Yellow: row.DaysYellow++; break;
                            case BufferColor.Green: row.DaysGreen++; break;
                            case BufferColor.Blue: row.DaysBlue++; break;
                            case BufferColor.NoColor: row.DaysNoColor++; break;
                        }
                    }

                    row.DaysRedAndBlack = row.DaysRed + row.DaysBlack;

                    row.DaysBlackPercentage = row.QuantityDays > 0 ? (decimal)row.DaysBlack / row.QuantityDays : 0;
                    row.DaysRedPercentage = row.QuantityDays > 0 ? (decimal)row.DaysRed / row.QuantityDays : 0;
                    row.DaysYellowPercentage = row.QuantityDays > 0 ? (decimal)row.DaysYellow / row.QuantityDays : 0;
                    row.DaysGreenPercentage = row.QuantityDays > 0 ? (decimal)row.DaysGreen / row.QuantityDays : 0;
                    row.DaysBluePercentage = row.QuantityDays > 0 ? (decimal)row.DaysBlue / row.QuantityDays : 0;
                    row.DaysNoColorPercentage = row.QuantityDays > 0 ? (decimal)row.DaysNoColor / row.QuantityDays : 0;
                    row.DaysRedAndBlackPercentage = row.QuantityDays > 0 ? (decimal)row.DaysRedAndBlack / row.QuantityDays : 0;

                    return row;
                })
                .OrderBy(r => r.IdCenter)
                .ThenBy(r => r.IdProduct)
                .ToList();

            return rows;
        }

        // Shared by GetBufferPenetrationAsync and GetItemsByBufferColorHistoryAsync — computes both the
        // Netflow and Execution buffer colors for a single History day's snapshot columns in one place, so the
        // two Netflow-vs-Execution formulas (see the comment above GetBufferPenetrationAsync) live in exactly
        // one spot instead of being duplicated per caller.
        private static (BufferColor Netflow, BufferColor Execution) ComputeBufferColors(
            decimal? stock, decimal? qualifiedDemand, decimal? openInbounds,
            decimal? redBaseZone, decimal? redSafeZone, decimal? yellowZone, decimal? greenZone)
        {
            var topOfRed = (redBaseZone ?? 0) + (redSafeZone ?? 0);

            var netflow = UtilsDdmrp.CalculateNetflow(stock ?? 0, qualifiedDemand ?? 0, openInbounds ?? 0);
            var topOfYellow = topOfRed + (yellowZone ?? 0);
            var topOfGreen = topOfYellow + (greenZone ?? 0);
            var netflowColor = UtilsDdmrp.CalculateBufferColor(netflow, topOfRed, topOfYellow, topOfGreen);

            var redZoneExecution = Math.Ceiling(topOfRed / 2);
            var yellowZoneExecution = redZoneExecution;
            var greenZoneExecution = yellowZone ?? 0;
            var topOfRedExecution = redZoneExecution;
            var topOfYellowExecution = redZoneExecution + yellowZoneExecution;
            var topOfGreenExecution = topOfYellowExecution + greenZoneExecution;
            var executionColor = UtilsDdmrp.CalculateBufferColor(stock ?? 0, topOfRedExecution, topOfYellowExecution, topOfGreenExecution);

            return (netflowColor, executionColor);
        }

        // One row per day, per perspective — for every day in the requested period that has at least one
        // History row (matching the optional idCenters/idProduct filters), counts how many items landed in
        // each of the 6 BufferColor values that day, from both perspectives at once (see ComputeBufferColors
        // above). Returned as two parallel lists (Netflow/Execution) rather than one flat row per (Date, Color)
        // — confirmed 2026-09-19 — so each day is always a single row with all 6 colors as columns (Red/
        // Yellow/Green/Blue/Black/NoColor), never a missing color for a day that has data (a color with no
        // matching item that day is simply 0). Unlike GetBufferPenetrationAsync (one row per item, days
        // aggregated into it), this report aggregates the other way: one row per day, items aggregated into it.
        public async Task<ItemsByBufferColorHistoryResult> GetItemsByBufferColorHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            CancellationToken cancellationToken = default)
        {
            var query = _context.History
                .Where(h => h.deletedAt == null
                    && h.Date >= dateStart && h.Date <= dateEnd
                    && h.Product.deletedAt == null
                    && h.Center.deletedAt == null);

            if (idCenters != null && idCenters.Length > 0)
                query = query.Where(h => idCenters.Contains(h.IdCenter));

            if (idProduct.HasValue)
                query = query.Where(h => h.IdProduct == idProduct.Value);

            var historyRows = await query
                .Select(h => new
                {
                    h.Date,
                    h.Stock,
                    h.QualifiedDemand,
                    h.OpenInbounds,
                    h.RedBaseZone,
                    h.RedSafeZone,
                    h.YellowZone,
                    h.GreenZone
                })
                .ToListAsync(cancellationToken);

            var itemColors = historyRows
                .Select(h =>
                {
                    var (netflowColor, executionColor) = ComputeBufferColors(
                        h.Stock, h.QualifiedDemand, h.OpenInbounds, h.RedBaseZone, h.RedSafeZone, h.YellowZone, h.GreenZone);
                    return new { h.Date, NetflowColor = netflowColor, ExecutionColor = executionColor };
                })
                .ToList();

            var result = new ItemsByBufferColorHistoryResult();

            foreach (var date in itemColors.Select(x => x.Date).Distinct().OrderBy(date => date))
            {
                var dayColors = itemColors.Where(x => x.Date == date).ToList();

                result.Netflow.Add(new BufferColorHistoryDayRow
                {
                    Date = date,
                    Red = dayColors.Count(x => x.NetflowColor == BufferColor.Red),
                    Yellow = dayColors.Count(x => x.NetflowColor == BufferColor.Yellow),
                    Green = dayColors.Count(x => x.NetflowColor == BufferColor.Green),
                    Blue = dayColors.Count(x => x.NetflowColor == BufferColor.Blue),
                    Black = dayColors.Count(x => x.NetflowColor == BufferColor.Black),
                    NoColor = dayColors.Count(x => x.NetflowColor == BufferColor.NoColor)
                });

                result.Execution.Add(new BufferColorHistoryDayRow
                {
                    Date = date,
                    Red = dayColors.Count(x => x.ExecutionColor == BufferColor.Red),
                    Yellow = dayColors.Count(x => x.ExecutionColor == BufferColor.Yellow),
                    Green = dayColors.Count(x => x.ExecutionColor == BufferColor.Green),
                    Blue = dayColors.Count(x => x.ExecutionColor == BufferColor.Blue),
                    Black = dayColors.Count(x => x.ExecutionColor == BufferColor.Black),
                    NoColor = dayColors.Count(x => x.ExecutionColor == BufferColor.NoColor)
                });
            }

            return result;
        }

        // One row per Date within the requested period, summing every metric across every History row
        // (any IdProduct, scoped to idCenters) that has RedBaseZone + RedSafeZone > 0 (nulls treated as 0,
        // same guard convention as ComputeBufferColors — an item with no red zone yet hasn't had its buffer
        // sized by the robot, so it's excluded rather than counted as 0). Unlike GetBufferPenetrationAsync/
        // GetItemsByBufferColorHistoryAsync, idCenters is required here, not an optional "all centers"
        // default — confirmed 2026-09-20, this report is always scoped to an explicit center list.
        // Per-row zone/Netflow math mirrors ComputeBufferColors (Execution zones = Ceiling(TopOfRed/2) twice
        // + YellowZone; Netflow = UtilsDdmrp.CalculateNetflow), but this report sums the raw zone sizes and
        // a handful of derived DDMRP metrics (AverageProjectedInventory, ExcessStock, oscillation range)
        // instead of classifying a color — see Formulas.md. Uses AvailableStock (Stock - ReservedStock, nulls
        // treated as 0) everywhere Stock would otherwise feed the math (Netflow, ExcessStock), same
        // Stock-minus-ReservedStock convention as InventoryBufferManagementRow's Netflow/CoverageDays.
        // RedSafeAnalytical/YellowSafeAnalytical/GreenAnalytical/YellowExcessAnalytical/RedExcessAnalytical
        // (added 2026-09-20) reuse GetInventoryBufferManagementQueryable's inline formulas for the same fields
        // (see withExecutionZones/withExecutionTops above), fed this method's netflowRedZone/netflowYellowZone/
        // netflowGreenZone instead of CenterProduct.RedZone/YellowZone/GreenZone.
        public async Task<List<AccumulatedBufferHistoryRow>> GetAccumulatedBufferHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[] idCenters,
            CancellationToken cancellationToken = default)
        {
            var historyRows = await _context.History
                .Where(h => h.deletedAt == null
                    && h.Date >= dateStart && h.Date <= dateEnd
                    && idCenters.Contains(h.IdCenter)
                    && h.Product.deletedAt == null
                    && h.Center.deletedAt == null
                    && ((h.RedBaseZone ?? 0) + (h.RedSafeZone ?? 0)) > 0)
                .Select(h => new
                {
                    h.Date,
                    h.Stock,
                    h.ReservedStock,
                    h.QualifiedDemand,
                    h.OpenInbounds,
                    h.RedBaseZone,
                    h.RedSafeZone,
                    h.YellowZone,
                    h.GreenZone
                })
                .ToListAsync(cancellationToken);

            var rows = historyRows
                .GroupBy(h => h.Date)
                .Select(group =>
                {
                    var row = new AccumulatedBufferHistoryRow { Date = group.Key };

                    foreach (var h in group)
                    {
                        var netflowRedZone = (h.RedBaseZone ?? 0) + (h.RedSafeZone ?? 0);
                        var netflowYellowZone = h.YellowZone ?? 0;
                        var netflowGreenZone = h.GreenZone ?? 0;
                        var availableStock = (h.Stock ?? 0) - (h.ReservedStock ?? 0);

                        var executionRedZone = Math.Ceiling(netflowRedZone / 2);
                        var executionYellowZone = executionRedZone;
                        var executionGreenZone = netflowYellowZone;

                        // Same formulas as CenterProduct.RedSafeAnalytical/YellowSafeAnalytical/GreenAnalytical/
                        // YellowExcessAnalytical/RedExcessAnalytical (as duplicated inline in
                        // GetInventoryBufferManagementQueryable above), fed netflowRedZone/netflowYellowZone/
                        // netflowGreenZone instead of CenterProduct's own RedZone/YellowZone/GreenZone.
                        var redSafeAnalytical = Math.Ceiling(netflowRedZone / 2);
                        var yellowSafeAnalytical = Math.Ceiling(netflowRedZone);
                        // Matches CenterProduct.GreenAnalytical (GreenZone alone, not RedZone + GreenZone).
                        var greenAnalytical = Math.Ceiling(netflowGreenZone);
                        var yellowExcessAnalytical = (netflowRedZone + netflowGreenZone) >= (netflowRedZone + netflowYellowZone)
                            ? 0
                            : Math.Ceiling((netflowRedZone + netflowYellowZone) - (netflowRedZone + netflowGreenZone));

                        var netflow = UtilsDdmrp.CalculateNetflow(availableStock, h.QualifiedDemand ?? 0, h.OpenInbounds ?? 0);
                        var averageProjectedInventory = netflowRedZone + (netflowGreenZone / 2);
                        var topOfGreenNetflow = netflowRedZone + netflowYellowZone + netflowGreenZone;
                        var excessStock = availableStock - topOfGreenNetflow > 0 ? availableStock - topOfGreenNetflow : 0;
                        var redExcessAnalytical = topOfGreenNetflow <= 0
                            ? 0
                            : Math.Ceiling(topOfGreenNetflow - (netflowRedZone + netflowGreenZone + yellowExcessAnalytical));

                        row.ExecutionRedZone += executionRedZone;
                        row.ExecutionYellowZone += executionYellowZone;
                        row.ExecutionGreenZone += executionGreenZone;
                        row.NetflowRedZone += netflowRedZone;
                        row.NetflowYellowZone += netflowYellowZone;
                        row.NetflowGreenZone += netflowGreenZone;
                        row.RedSafeAnalytical += redSafeAnalytical;
                        row.YellowSafeAnalytical += yellowSafeAnalytical;
                        row.GreenAnalytical += greenAnalytical;
                        row.YellowExcessAnalytical += yellowExcessAnalytical;
                        row.RedExcessAnalytical += redExcessAnalytical;
                        row.AverageProjectedInventory += averageProjectedInventory;
                        row.AvailableStock += availableStock;
                        row.Netflow += netflow;
                        row.ExcessStock += excessStock;
                        row.MinimumOscillationRange += netflowRedZone;
                        row.MaximumOscillationRange += netflowRedZone + netflowGreenZone;
                    }

                    return row;
                })
                .OrderBy(r => r.Date)
                .ToList();

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
