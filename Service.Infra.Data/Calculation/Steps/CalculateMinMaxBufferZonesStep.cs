using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class CalculateMinMaxBufferZonesStep : ICalculationStep
    {
        private const int DefaultThresholdDays = 180;

        private readonly ApplicationDbContext _context;

        public CalculateMinMaxBufferZonesStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateMinMaxBufferZones", StringComparison.OrdinalIgnoreCase);

        public async Task<CalculationStepResult> ExecuteAsync(CalculationStepConfig step, int? idCenterProduct, CancellationToken cancellationToken = default)
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
                await _context.Database.ExecuteSqlInterpolatedAsync(BuildSql(thresholdDays, idCenterProduct), cancellationToken);
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = true, DurationMs = stopwatch.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new CalculationStepResult { Name = step.Name, Success = false, Error = ex.Message, DurationMs = stopwatch.ElapsedMilliseconds };
            }
        }

        private static FormattableString BuildSql(int thresholdDays, int? idCenterProduct) => $"""
            UPDATE cp
            SET cp.YellowZone = 0, cp.GreenZone = 0, cp.RedZoneSafe = 0, cp.RedZoneBase = 0
            FROM dbo.CenterProducts cp
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 2 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});

            DECLARE @Today DATE = CAST(GETDATE() AS DATE);
            DECLARE @WindowStart DATE = DATEADD(DAY, -{thresholdDays}, @Today);

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
                WHERE cp.deletedAt IS NULL AND cp.BufferType = 2 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            ),
            MaxOutflow AS (
                SELECT h.IdProduct, h.IdCenter, MAX(h.Consumption) AS Value
                FROM dbo.Histories h
                WHERE h.deletedAt IS NULL
                  AND h.DiscardStatus <> 2
                  AND h.Date >= @WindowStart
                  AND h.Date < @Today
                GROUP BY h.IdProduct, h.IdCenter
            ),
            Zones AS (
                SELECT
                    cp.Id AS CenterProductId,
                    (SELECT MAX(v) FROM (VALUES
                        (IIF(cp.GreenZoneParametrizationUseMoq = 1, cp.Moq, 0)),
                        (IIF(cp.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = 1, cp.Adu * cp.LeadTime * (CASE WHEN cp.UseSuggestedLTFactor = 1 THEN bp.LeadTimeFactor ELSE cp.CustomLeadTimeFactor END), 0)),
                        (IIF(cp.GreenZoneParametrizationUseAduXFrequency = 1, cp.Frequency * (CASE WHEN cp.UseDafOnGreenZone = 1 THEN a.Value ELSE cp.Adu END), 0))
                    ) AS g(v)) AS Green,
                    ISNULL(mo.Value, 0) AS RedBase
                FROM dbo.CenterProducts cp
                LEFT JOIN dbo.BufferProfiles bp ON bp.Id = cp.IdBufferProfile
                JOIN AdjustedAdu a ON a.CenterProductId = cp.Id
                LEFT JOIN MaxOutflow mo ON mo.IdProduct = cp.IdProduct AND mo.IdCenter = cp.IdCenter
                WHERE cp.deletedAt IS NULL AND cp.BufferType = 2 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct})
            )
            UPDATE cp
            SET
                cp.GreenZone = CEILING(IIF(z.Green < 0, 0, z.Green)),
                cp.RedZoneBase = CEILING(IIF(z.RedBase < 0, 0, z.RedBase))
            FROM dbo.CenterProducts cp
            JOIN Zones z ON z.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 2 AND ({idCenterProduct} IS NULL OR cp.Id = {idCenterProduct});
            """;
    }
}
