using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class ReplicateCenterProductToHistoryStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public ReplicateCenterProductToHistoryStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "ReplicateCenterProductToHistory", StringComparison.OrdinalIgnoreCase);

        public async Task<CalculationStepResult> ExecuteAsync(CalculationStepConfig step, int? idCenterProduct, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(BuildSql(idCenterProduct), cancellationToken);
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = false, Error = ex.Message, DurationMs = stopwatch.ElapsedMilliseconds };
            }
        }

        // Upserts today's History row per active CenterProduct with the robot-only snapshot columns
        // (see CLAUDE.md/Formulas.md) — never touches Consumption/DiscardStatus, which belong to the
        // ingestion pipeline (client-fed consumption data), not this step. Must run LAST in
        // calculation.config.json: it snapshots the final, post-ZAF/QualifiedDemand state of CenterProduct.
        // idCenterProduct: see CalculateAduStandardDesvAndCvStep.
        private static FormattableString BuildSql(int? idCenterProduct) => $"""
            MERGE INTO dbo.Histories AS target
            USING (
                SELECT
                    cp.IdProduct,
                    cp.IdCenter,
                    CAST(GETDATE() AS DATE) AS [Date],
                    cp.Adi,
                    cp.Adu,
                    cp.Cv,
                    cp.Frequency,
                    cp.FutureAduDays,
                    cp.GreenZone,
                    cp.HistoryAduDays,
                    cp.IdBufferProfile,
                    cp.IdReason,
                    cp.IdTag,
                    cp.LeadTime,
                    cp.Moq,
                    cp.PackQuantity,
                    cp.QualifiedDemand,
                    cp.RedZoneBase AS RedBaseZone,
                    cp.RedZoneSafe AS RedSafeZone,
                    cp.ReservedStock,
                    cp.StandardDeviation,
                    cp.Stock,
                    cp.YellowZone,
                    cp.ZafGreenZone,
                    cp.ZafRedZone,
                    cp.ZafYellowZone,
                    ISNULL((
                        SELECT SUM(o.Quantity - o.DeliveredQuantity)
                        FROM dbo.Orders o
                        WHERE o.deletedAt IS NULL
                          AND o.IsInbound = 1
                          AND o.IsFictional = 0
                          AND o.IdDestinyCenter = cp.IdCenter
                          AND o.IdProduct = cp.IdProduct
                    ), 0) AS OpenInbounds,
                    ISNULL((
                        SELECT SUM(o.Quantity - o.DeliveredQuantity)
                        FROM dbo.Orders o
                        WHERE o.deletedAt IS NULL
                          AND o.IsOutbound = 1
                          AND o.IsFictional = 0
                          AND o.IdOriginCenter = cp.IdCenter
                          AND o.IdProduct = cp.IdProduct
                    ), 0) AS OpenOutbound
                FROM dbo.CenterProducts cp
                WHERE cp.deletedAt IS NULL
                  AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            ) AS source
            ON target.IdProduct = source.IdProduct
               AND target.IdCenter = source.IdCenter
               AND target.Date = source.[Date]
               AND target.deletedAt IS NULL
            WHEN MATCHED THEN
                UPDATE SET
                    target.Adi = source.Adi,
                    target.Adu = source.Adu,
                    target.Cv = source.Cv,
                    target.Frequency = source.Frequency,
                    target.FutureAduDays = source.FutureAduDays,
                    target.GreenZone = source.GreenZone,
                    target.HistoryAduDays = source.HistoryAduDays,
                    target.IdBufferProfile = source.IdBufferProfile,
                    target.IdReason = source.IdReason,
                    target.IdTag = source.IdTag,
                    target.LeadTime = source.LeadTime,
                    target.Moq = source.Moq,
                    target.OpenInbounds = source.OpenInbounds,
                    target.OpenOutbound = source.OpenOutbound,
                    target.PackQuantity = source.PackQuantity,
                    target.QualifiedDemand = source.QualifiedDemand,
                    target.RedBaseZone = source.RedBaseZone,
                    target.RedSafeZone = source.RedSafeZone,
                    target.ReservedStock = source.ReservedStock,
                    target.StandardDeviation = source.StandardDeviation,
                    target.Stock = source.Stock,
                    target.YellowZone = source.YellowZone,
                    target.ZafGreenZone = source.ZafGreenZone,
                    target.ZafRedZone = source.ZafRedZone,
                    target.ZafYellowZone = source.ZafYellowZone
            WHEN NOT MATCHED THEN
                INSERT (
                    IdProduct, IdCenter, Consumption, Date, DiscardStatus,
                    Adi, Adu, Cv, Frequency, FutureAduDays, GreenZone, HistoryAduDays,
                    IdBufferProfile, IdReason, IdTag, LeadTime, Moq, OpenInbounds, OpenOutbound,
                    PackQuantity, QualifiedDemand, RedBaseZone, RedSafeZone, ReservedStock, StandardDeviation,
                    Stock, YellowZone, ZafGreenZone, ZafRedZone, ZafYellowZone
                )
                VALUES (
                    source.IdProduct, source.IdCenter, 0, source.[Date], 0,
                    source.Adi, source.Adu, source.Cv, source.Frequency, source.FutureAduDays, source.GreenZone, source.HistoryAduDays,
                    source.IdBufferProfile, source.IdReason, source.IdTag, source.LeadTime, source.Moq, source.OpenInbounds, source.OpenOutbound,
                    source.PackQuantity, source.QualifiedDemand, source.RedBaseZone, source.RedSafeZone, source.ReservedStock, source.StandardDeviation,
                    source.Stock, source.YellowZone, source.ZafGreenZone, source.ZafRedZone, source.ZafYellowZone
                );
            """;
    }
}
