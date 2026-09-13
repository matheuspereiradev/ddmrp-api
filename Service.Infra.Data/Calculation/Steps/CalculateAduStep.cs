using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    // See Formulas.md for the business rule this implements (Adu: Historico/Futuro/Misto).
    public class CalculateAduStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public CalculateAduStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateAdu", StringComparison.OrdinalIgnoreCase);

        public async Task<CalculationStepResult> ExecuteAsync(CalculationStepConfig step, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _context.Database.ExecuteSqlRawAsync(Sql, cancellationToken);
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = false, Error = ex.Message, DurationMs = stopwatch.ElapsedMilliseconds };
            }
        }

        // DiscardStatus 2 = Discarded (Service.Domain.Enums.DiscardStatus).
        private const string Sql = """
            DECLARE @Today DATE = CAST(GETDATE() AS DATE);

            ;WITH RankedHistory AS (
                SELECT
                    h.IdProduct,
                    h.IdCenter,
                    h.Quantity,
                    ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
                FROM dbo.Histories h
                WHERE h.deletedAt IS NULL
                  AND h.DiscardStatus <> 2
                  AND h.Date < @Today
            ),
            HistoricalAdu AS (
                SELECT
                    cp.Id AS CenterProductId,
                    ISNULL(SUM(rh.Quantity), 0) / cp.HistoryAduDays AS Value
                FROM dbo.CenterProducts cp
                LEFT JOIN RankedHistory rh
                    ON rh.IdProduct = cp.IdProduct
                    AND rh.IdCenter = cp.IdCenter
                    AND rh.rn <= cp.HistoryAduDays
                WHERE cp.deletedAt IS NULL
                  AND ISNULL(cp.HistoryAduDays, 0) > 0
                GROUP BY cp.Id, cp.HistoryAduDays
            ),
            FutureAdu AS (
                SELECT
                    cp.Id AS CenterProductId,
                    ISNULL(SUM(f.Quantity), 0) / cp.FutureAduDays AS Value
                FROM dbo.CenterProducts cp
                LEFT JOIN dbo.Forecasts f
                    ON f.IdProduct = cp.IdProduct
                    AND f.IdCenter = cp.IdCenter
                    AND f.deletedAt IS NULL
                    AND f.Date > @Today
                    AND f.Date <= DATEADD(DAY, cp.FutureAduDays, @Today)
                WHERE cp.deletedAt IS NULL
                  AND ISNULL(cp.FutureAduDays, 0) > 0
                GROUP BY cp.Id, cp.FutureAduDays
            )
            UPDATE cp
            SET cp.Adu = CASE
                    WHEN ISNULL(cp.HistoryAduDays, 0) > 0 AND ISNULL(cp.FutureAduDays, 0) = 0
                        THEN ha.Value
                    WHEN ISNULL(cp.HistoryAduDays, 0) = 0 AND ISNULL(cp.FutureAduDays, 0) > 0
                        THEN fa.Value
                    WHEN ISNULL(cp.HistoryAduDays, 0) > 0 AND ISNULL(cp.FutureAduDays, 0) > 0
                         AND ha.Value IS NOT NULL AND fa.Value IS NOT NULL
                        THEN (ha.Value + fa.Value) / 2
                    ELSE NULL
                END
            FROM dbo.CenterProducts cp
            LEFT JOIN HistoricalAdu ha ON ha.CenterProductId = cp.Id
            LEFT JOIN FutureAdu fa ON fa.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL;
            """;
    }
}
