using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class ApplyBafStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public ApplyBafStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "ApplyBAF", StringComparison.OrdinalIgnoreCase);

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

        // idCenterProduct: see CalculateAduStandardDesvAndCvStep. The 2nd statement (AlreadyReverted
        // bookkeeping) has no CenterProducts join of its own, so it's scoped via an EXISTS against the
        // one target CenterProduct's (IdProduct, IdCenter) instead of a direct cp.Id filter.
        private static FormattableString BuildSql(int? idCenterProduct) => $"""
            UPDATE cp
            SET
                cp.BufferType = ISNULL(baf.BufferTypeOld, cp.BufferType),
                cp.RedZoneSafe = ISNULL(baf.BufferDdmrpRedSafeOld, cp.RedZoneSafe),
                cp.RedZoneBase = ISNULL(baf.BufferDdmrpRedBaseOld, cp.RedZoneBase),
                cp.YellowZone = ISNULL(baf.BufferDdmrpYellowOld, cp.YellowZone),
                cp.GreenZone = ISNULL(baf.BufferDdmrpGreenOld, cp.GreenZone)
            FROM dbo.CenterProducts cp
            JOIN dbo.BufferAdjustmentFactors baf
                ON baf.IdProduct = cp.IdProduct AND baf.IdCenter = cp.IdCenter
            WHERE cp.deletedAt IS NULL
              AND baf.deletedAt IS NULL
              AND baf.EffectiveTo < GETDATE()
              AND baf.AlreadyReverted = 0
              AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            UPDATE baf
            SET baf.AlreadyReverted = 1
            FROM dbo.BufferAdjustmentFactors baf
            WHERE baf.deletedAt IS NULL
              AND baf.EffectiveTo < GETDATE()
              AND baf.AlreadyReverted = 0
              AND ({idCenterProduct} IS NULL OR EXISTS (
                    SELECT 1 FROM dbo.CenterProducts cp2
                    WHERE cp2.Id = {idCenterProduct} AND cp2.IdProduct = baf.IdProduct AND cp2.IdCenter = baf.IdCenter
              ));

            UPDATE cp
            SET cp.BufferType = baf.BufferType
            FROM dbo.CenterProducts cp
            JOIN dbo.BufferAdjustmentFactors baf
                ON baf.IdProduct = cp.IdProduct AND baf.IdCenter = cp.IdCenter
            WHERE cp.deletedAt IS NULL
              AND baf.deletedAt IS NULL
              AND baf.IsActive = 1
              AND baf.EffectiveFrom <= GETDATE()
              AND baf.EffectiveTo >= GETDATE()
              AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            UPDATE cp
            SET
                cp.YellowZone = baf.BufferDdmrpYellow,
                cp.GreenZone = baf.BufferDdmrpGreen,
                cp.RedZoneSafe = CEILING(baf.BufferDdmrpRed / 2),
                cp.RedZoneBase = CEILING(baf.BufferDdmrpRed / 2)
            FROM dbo.CenterProducts cp
            JOIN dbo.BufferAdjustmentFactors baf
                ON baf.IdProduct = cp.IdProduct AND baf.IdCenter = cp.IdCenter
            WHERE cp.deletedAt IS NULL
              AND baf.deletedAt IS NULL
              AND baf.IsActive = 1
              AND baf.EffectiveFrom <= GETDATE()
              AND baf.EffectiveTo >= GETDATE()
              AND baf.BufferType = 1
              AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
