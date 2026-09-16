using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    // See Formulas.md for the business rule this implements (Adu: Historico/Futuro/Misto;
    // StandardDeviation/Cv: same HistoryAduDays window as Adu's Historico).
    public class CalculateAduStandardDesvAndCvStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public CalculateAduStandardDesvAndCvStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateAduStandardDesvAndCv", StringComparison.OrdinalIgnoreCase);

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

        // DiscardStatus 2 = Discarded (Service.Domain.Enums.DiscardStatus).
        // idCenterProduct: when set, scopes the whole step to that one CenterProduct.Id (POST /api/calculation/run's
        // optional recalculation-for-one-item mode — see CLAUDE.md); null runs it for every active CenterProduct.
        private static FormattableString BuildSql(int? idCenterProduct) => $"""
            UPDATE dbo.CenterProducts
            SET Adu = 0, StandardDeviation = 0, Cv = 0
            WHERE deletedAt IS NULL AND ({idCenterProduct} IS NULL OR Id = {idCenterProduct});

            DECLARE @Today DATE = CAST(GETDATE() AS DATE);

            ;WITH RankedHistory AS (
                SELECT
                    h.IdProduct,
                    h.IdCenter,
                    h.Consumption,
                    ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
                FROM dbo.Histories h
                WHERE h.deletedAt IS NULL
                  AND h.DiscardStatus <> 2
                  AND h.Date < @Today
            ),
            HistoricalStats AS (
                SELECT
                    cp.Id AS CenterProductId,
                    ISNULL(SUM(rh.Consumption), 0) / cp.HistoryAduDays AS HistoricalValue,
                    CAST(STDEVP(ISNULL(rh.Consumption, 0)) AS DECIMAL(18, 4)) AS StdDevValue,
                    CAST(AVG(ISNULL(rh.Consumption, 0)) AS DECIMAL(18, 4)) AS AvgValue
                FROM dbo.CenterProducts cp
                LEFT JOIN RankedHistory rh
                    ON rh.IdProduct = cp.IdProduct
                    AND rh.IdCenter = cp.IdCenter
                    AND rh.rn <= cp.HistoryAduDays
                WHERE cp.deletedAt IS NULL
                  AND ISNULL(cp.HistoryAduDays, 0) > 0
                  AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
                GROUP BY cp.Id, cp.HistoryAduDays
            ),
            ForecastBusinessDays AS (
                -- How many business days each Forecast's own [StartDate, EndDate] window covers —
                -- its Value is split evenly across exactly those days (Forecast is monthly/interval-based,
                -- not one row per day — see CLAUDE.md's Forecast bullet).
                SELECT
                    f.Id AS ForecastId,
                    COUNT(wc.Date) AS BusinessDayCount
                FROM dbo.Forecasts f
                JOIN dbo.Calendar wc
                    ON wc.Date >= f.StartDate AND wc.Date <= f.EndDate AND wc.IsWorkingDay = 1
                WHERE f.deletedAt IS NULL
                GROUP BY f.Id
            ),
            FutureAdu AS (
                SELECT
                    cp.Id AS CenterProductId,
                    ISNULL(SUM(
                        CASE WHEN wc.IsWorkingDay = 1 AND ISNULL(fbd.BusinessDayCount, 0) > 0
                            THEN f.Value / fbd.BusinessDayCount
                            ELSE 0
                        END
                    ), 0) / cp.FutureAduDays AS Value
                FROM dbo.CenterProducts cp
                JOIN dbo.Calendar wc
                    ON wc.Date > @Today AND wc.Date <= DATEADD(DAY, cp.FutureAduDays, @Today)
                LEFT JOIN dbo.Forecasts f
                    ON f.IdProduct = cp.IdProduct
                    AND f.IdCenter = cp.IdCenter
                    AND f.deletedAt IS NULL
                    AND wc.Date >= f.StartDate AND wc.Date <= f.EndDate
                LEFT JOIN ForecastBusinessDays fbd ON fbd.ForecastId = f.Id
                WHERE cp.deletedAt IS NULL
                  AND ISNULL(cp.FutureAduDays, 0) > 0
                  AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
                GROUP BY cp.Id, cp.FutureAduDays
            )
            UPDATE cp
            SET cp.Adu = CASE
                    WHEN ISNULL(cp.HistoryAduDays, 0) > 0 AND ISNULL(cp.FutureAduDays, 0) = 0
                        THEN hs.HistoricalValue
                    WHEN ISNULL(cp.HistoryAduDays, 0) = 0 AND ISNULL(cp.FutureAduDays, 0) > 0
                        THEN fa.Value
                    WHEN ISNULL(cp.HistoryAduDays, 0) > 0 AND ISNULL(cp.FutureAduDays, 0) > 0
                        THEN (hs.HistoricalValue + fa.Value) / 2
                    ELSE NULL
                END,
                cp.StandardDeviation = ISNULL(hs.StdDevValue, 0),
                cp.Cv = CASE
                    WHEN ISNULL(hs.AvgValue, 0) = 0 THEN 0
                    ELSE hs.StdDevValue / hs.AvgValue
                END
            FROM dbo.CenterProducts cp
            LEFT JOIN HistoricalStats hs ON hs.CenterProductId = cp.Id
            LEFT JOIN FutureAdu fa ON fa.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
