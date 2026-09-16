using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class CalculateQualifiedDemandStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public CalculateQualifiedDemandStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateQualifiedDemand", StringComparison.OrdinalIgnoreCase);

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

        // idCenterProduct: see CalculateAduStandardDesvAndCvStep.
        private static FormattableString BuildSql(int? idCenterProduct) => $"""
            UPDATE cp
            SET cp.QualifiedDemand = 0
            FROM dbo.CenterProducts cp
            WHERE cp.deletedAt IS NULL AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            ;WITH TodayPending AS (
                SELECT
                    x.IdProduct,
                    x.IdOriginCenter,
                    CAST(GETDATE() AS DATE) AS DeliveryDate,
                    SUM(x.PendingQuantity) AS PendingQuantity
                FROM (
                    SELECT o.IdProduct, o.IdOriginCenter, (o.Quantity - o.DeliveredQuantity) AS PendingQuantity
                    FROM dbo.Orders o
                    WHERE o.deletedAt IS NULL
                      AND o.IsOutbound = 1
                      AND o.IsFictional = 0
                      AND CAST(o.DeliveryDate AS DATE) <= CAST(GETDATE() AS DATE)
                    UNION ALL
                    SELECT o.IdProduct, o.IdOriginCenter, (o.Quantity - o.DeliveredQuantity) AS PendingQuantity
                    FROM dbo.Orders o
                    WHERE o.deletedAt IS NULL
                      AND o.IsOutbound = 1
                      AND o.IsFictional = 1
                      AND CAST(o.DeliveryDate AS DATE) = CAST(GETDATE() AS DATE)
                ) x
                GROUP BY x.IdProduct, x.IdOriginCenter
            ),
            FuturePending AS (
                SELECT
                    o.IdProduct,
                    o.IdOriginCenter,
                    CAST(o.DeliveryDate AS DATE) AS DeliveryDate,
                    SUM(o.Quantity - o.DeliveredQuantity) AS PendingQuantity
                FROM dbo.Orders o
                WHERE o.deletedAt IS NULL
                  AND o.IsOutbound = 1
                  AND CAST(o.DeliveryDate AS DATE) > CAST(GETDATE() AS DATE)
                GROUP BY o.IdProduct, o.IdOriginCenter, CAST(o.DeliveryDate AS DATE)
            ),
            PendingByDay AS (
                SELECT * FROM TodayPending
                UNION ALL
                SELECT * FROM FuturePending
            ),
            QualifiedByDay AS (
                SELECT
                    cp.Id AS CenterProductId,
                    IIF(
                        cp.SpikeThresholdType = 1 AND po.PendingQuantity >= cp.Adu * cp.SpikeThresholdAdu,
                        po.PendingQuantity,
                        IIF(
                            cp.SpikeThresholdType = 0 AND po.PendingQuantity >= (ISNULL(cp.RedZoneBase, 0) + ISNULL(cp.RedZoneSafe, 0)) * cp.SpikeThresholdPercentageRedZone,
                            po.PendingQuantity,
                            0
                        )
                    ) AS QualifiedQuantity
                FROM dbo.CenterProducts cp
                INNER JOIN PendingByDay po
                    ON po.IdProduct = cp.IdProduct
                   AND po.IdOriginCenter = cp.IdCenter
                WHERE cp.deletedAt IS NULL
                  AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
                  AND po.DeliveryDate <= DATEADD(
                        DAY,
                        IIF(cp.SpikeHorizonType = 0, cp.LeadTime * cp.SpikeHorizonLTDays, cp.SpikeHorizonValue),
                        CAST(GETDATE() AS DATE)
                    )
            ),
            QualifiedTotals AS (
                SELECT CenterProductId, SUM(QualifiedQuantity) AS TotalQualifiedDemand
                FROM QualifiedByDay
                GROUP BY CenterProductId
            )
            UPDATE cp
            SET cp.QualifiedDemand = qt.TotalQualifiedDemand
            FROM dbo.CenterProducts cp
            INNER JOIN QualifiedTotals qt ON qt.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
