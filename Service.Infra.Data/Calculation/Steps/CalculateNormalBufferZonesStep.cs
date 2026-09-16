using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    // See Formulas.md for the business rule this implements (Red/Yellow/Green buffer zones,
    // BufferType.Normal only — see Service.Domain.Enums.BufferType).
    public class CalculateNormalBufferZonesStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public CalculateNormalBufferZonesStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateNormalBufferZones", StringComparison.OrdinalIgnoreCase);

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

        // BufferType 0 = Normal (Service.Domain.Enums.BufferType). Depends on CenterProduct.Adu
        // already being computed, so this step must run after CalculateAduStandardDesvAndCv in
        // calculation.config.json. AdjustmentType 0 = FlatValue, 1 = Percentage
        // (Service.Domain.Enums.AdjustmentType). idCenterProduct: see CalculateAduStandardDesvAndCvStep.
        private static FormattableString BuildSql(int? idCenterProduct) => $"""
            UPDATE cp
            SET cp.YellowZone = 0, cp.GreenZone = 0, cp.RedZoneSafe = 0, cp.RedZoneBase = 0
            FROM dbo.CenterProducts cp
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 0 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            ;WITH ActiveDaf AS (
                SELECT daf.IdProduct, daf.IdCenter, daf.AdjustmentType, daf.AdjustmentValue
                FROM dbo.DemandAdjustmentFactors daf
                WHERE daf.deletedAt IS NULL
                  AND daf.IsActive = 1
                  AND daf.EffectiveFrom <= GETDATE()
                  AND daf.EffectiveTo >= GETDATE()
            ),
            AdjustedAdu AS (
                SELECT
                    cp.Id AS CenterProductId,
                    CASE
                        WHEN daf.IdProduct IS NULL THEN cp.Adu
                        WHEN daf.AdjustmentType = 0 THEN daf.AdjustmentValue + cp.Adu
                        WHEN daf.AdjustmentType = 1 THEN daf.AdjustmentValue * cp.Adu
                    END AS Value
                FROM dbo.CenterProducts cp
                LEFT JOIN ActiveDaf daf ON daf.IdProduct = cp.IdProduct AND daf.IdCenter = cp.IdCenter
                WHERE cp.deletedAt IS NULL AND cp.BufferType = 0 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            ),
            Zones AS (
                SELECT
                    cp.Id AS CenterProductId,
                    a.Value * cp.LeadTime AS Yellow,
                    (SELECT MAX(v) FROM (VALUES
                        (IIF(cp.GreenZoneParametrizationUseMoq = 1, cp.Moq, 0)),
                        (IIF(cp.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = 1, cp.Adu * cp.LeadTime * (CASE WHEN cp.UseSuggestedLTFactor = 1 THEN bp.LeadTimeFactor ELSE cp.CustomLeadTimeFactor END), 0)),
                        (IIF(cp.GreenZoneParametrizationUseAduXFrequency = 1, cp.Frequency * (CASE WHEN cp.UseDafOnGreenZone = 1 THEN a.Value ELSE cp.Adu END), 0))
                    ) AS g(v)) AS Green,
                    a.Value * cp.LeadTime
                        * (CASE WHEN cp.UseSuggestedLTFactor = 1 THEN bp.LeadTimeFactor ELSE cp.CustomLeadTimeFactor END) AS RedSafe,
                    a.Value * cp.LeadTime
                        * (CASE WHEN cp.UseSuggestedLTFactor = 1 THEN bp.LeadTimeFactor ELSE cp.CustomLeadTimeFactor END)
                        * (CASE WHEN cp.UseSuggestedVariabilityFactor = 1 THEN bp.VariabilityFactor ELSE cp.CustomVariabilityFactor END) AS RedBase
                FROM dbo.CenterProducts cp
                LEFT JOIN dbo.BufferProfiles bp ON bp.Id = cp.IdBufferProfile
                JOIN AdjustedAdu a ON a.CenterProductId = cp.Id
                WHERE cp.deletedAt IS NULL AND cp.BufferType = 0 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            )
            UPDATE cp
            SET
                cp.YellowZone = CEILING(IIF(z.Yellow < 0, 0, z.Yellow)),
                cp.GreenZone = CEILING(IIF(z.Green < 0, 0, z.Green)),
                cp.RedZoneSafe = CEILING(IIF(z.RedSafe < 0, 0, z.RedSafe)),
                cp.RedZoneBase = CEILING(IIF(z.RedBase < 0, 0, z.RedBase))
            FROM dbo.CenterProducts cp
            JOIN Zones z ON z.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 0 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
