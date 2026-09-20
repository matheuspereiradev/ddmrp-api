using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class ApplyZafStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public ApplyZafStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "ApplyZAF", StringComparison.OrdinalIgnoreCase);

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
            SET cp.ZafRedZone = 0, cp.ZafYellowZone = 0, cp.ZafGreenZone = 0
            FROM dbo.CenterProducts cp
            WHERE cp.deletedAt IS NULL AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            ;WITH ActiveZaf AS (
                SELECT zaf.IdProduct, zaf.IdCenter, zaf.TargetZone, zaf.AdjustmentType, zaf.AdjustmentValue
                FROM dbo.ZoneAdjustmentFactors zaf
                WHERE zaf.deletedAt IS NULL
                  AND zaf.IsActive = 1
                  AND zaf.EffectiveFrom <= GETDATE()
                  AND zaf.EffectiveTo >= GETDATE()
            ),
            Deltas AS (
                SELECT
                    cp.Id AS CenterProductId,
                    ISNULL(
                        CASE
                            WHEN zg.AdjustmentType = 0 THEN zg.AdjustmentValue
                            WHEN zg.AdjustmentType = 1 THEN cp.GreenZone * zg.AdjustmentValue
                        END, 0) AS GreenDelta,
                    ISNULL(
                        CASE
                            WHEN zy.AdjustmentType = 0 THEN zy.AdjustmentValue
                            WHEN zy.AdjustmentType = 1 THEN cp.YellowZone * zy.AdjustmentValue
                        END, 0) AS YellowDelta,
                    ISNULL(
                        CASE
                            WHEN zr.AdjustmentType = 0 THEN zr.AdjustmentValue
                            WHEN zr.AdjustmentType = 1 THEN (cp.RedZoneSafe + cp.RedZoneBase) * zr.AdjustmentValue
                        END, 0) AS RedDelta
                FROM dbo.CenterProducts cp
                LEFT JOIN ActiveZaf zg ON zg.IdProduct = cp.IdProduct AND zg.IdCenter = cp.IdCenter AND zg.TargetZone = 2
                LEFT JOIN ActiveZaf zy ON zy.IdProduct = cp.IdProduct AND zy.IdCenter = cp.IdCenter AND zy.TargetZone = 1
                LEFT JOIN ActiveZaf zr ON zr.IdProduct = cp.IdProduct AND zr.IdCenter = cp.IdCenter AND zr.TargetZone = 0
                WHERE cp.deletedAt IS NULL AND cp.BufferType <> 1 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            )
            UPDATE cp
            SET
                cp.ZafGreenZone = d.GreenDelta,
                cp.ZafYellowZone = d.YellowDelta,
                cp.ZafRedZone = d.RedDelta,
                cp.GreenZone = cp.GreenZone + d.GreenDelta,
                cp.YellowZone = cp.YellowZone + d.YellowDelta,
                cp.RedZoneBase = cp.RedZoneBase + d.RedDelta
            FROM dbo.CenterProducts cp
            JOIN Deltas d ON d.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND cp.BufferType <> 1 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
