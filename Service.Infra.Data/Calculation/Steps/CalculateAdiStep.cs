using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    // See Formulas.md for the business rule this implements (Adi: Average Demand Interval).
    public class CalculateAdiStep : ICalculationStep
    {
        private const int DefaultThresholdDays = 360;

        private readonly ApplicationDbContext _context;

        public CalculateAdiStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateAdi", StringComparison.OrdinalIgnoreCase);

        public async Task<CalculationStepResult> ExecuteAsync(CalculationStepConfig step, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var thresholdParameter = step.Parameters.FirstOrDefault(p => string.Equals(p.Name, "ThresholdDays", StringComparison.OrdinalIgnoreCase));
            var thresholdDays = DefaultThresholdDays;
            if (thresholdParameter != null && !int.TryParse(thresholdParameter.Value, out thresholdDays))
            {
                stopwatch.Stop();
                return new CalculationStepResult
                {
                    Name = step.Name,
                    Success = false,
                    Error = $"Invalid 'ThresholdDays' parameter value '{thresholdParameter.Value}'.",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(BuildSql(thresholdDays), cancellationToken);
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = false, Error = ex.Message, DurationMs = stopwatch.ElapsedMilliseconds };
            }
        }

        // Adi = (total de linhas de History no período) / (linhas com Quantity > 0) — sem filtro de
        // DiscardStatus (todas as linhas contam) e sem denominador zero: sem nenhuma linha com
        // Quantity > 0 no período, Adi = 0.
        private static FormattableString BuildSql(int thresholdDays) => $"""
            DECLARE @Today DATE = CAST(GETDATE() AS DATE);
            DECLARE @WindowStart DATE = DATEADD(DAY, -{thresholdDays}, @Today);

            ;WITH HistoryWindow AS (
                SELECT h.IdProduct, h.IdCenter, h.Quantity
                FROM dbo.Histories h
                WHERE h.deletedAt IS NULL
                  AND h.Date >= @WindowStart
                  AND h.Date < @Today
            ),
            AdiAgg AS (
                SELECT
                    IdProduct,
                    IdCenter,
                    COUNT(*) AS TotalRecords,
                    SUM(CASE WHEN Quantity > 0 THEN 1 ELSE 0 END) AS PositiveRecords
                FROM HistoryWindow
                GROUP BY IdProduct, IdCenter
            )
            UPDATE cp
            SET cp.Adi = CASE
                    WHEN ISNULL(aa.PositiveRecords, 0) = 0 THEN 0
                    ELSE CAST(aa.TotalRecords AS DECIMAL(18,4)) / aa.PositiveRecords
                END
            FROM dbo.CenterProducts cp
            LEFT JOIN AdiAgg aa ON aa.IdProduct = cp.IdProduct AND aa.IdCenter = cp.IdCenter
            WHERE cp.deletedAt IS NULL;
            """;
    }
}
