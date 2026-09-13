using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Calculation;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Calculation.Steps
{
    public class CalculateDynamicMinMaxBufferZonesStep : ICalculationStep
    {
        private readonly ApplicationDbContext _context;

        public CalculateDynamicMinMaxBufferZonesStep(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CanHandle(string name) => string.Equals(name, "CalculateDynamicMinMaxBufferZones", StringComparison.OrdinalIgnoreCase);

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

        private const string Sql = """
            UPDATE cp
            SET cp.YellowZone = 0, cp.GreenZone = 0, cp.RedZoneSafe = 0, cp.RedZoneBase = 0
            FROM dbo.CenterProducts cp
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 3;

            DECLARE @Today DATE = CAST(GETDATE() AS DATE);

            ;WITH ValidRanked AS (
                SELECT h.IdProduct, h.IdCenter, h.Date,
                    ROW_NUMBER() OVER (PARTITION BY h.IdProduct, h.IdCenter ORDER BY h.Date DESC) AS rn
                FROM dbo.Histories h
                WHERE h.deletedAt IS NULL AND h.DiscardStatus <> 2 AND h.Date < @Today
            ),
            WindowStart AS (
                SELECT cp.Id AS CenterProductId, MIN(vr.Date) AS WindowStartDate
                FROM dbo.CenterProducts cp
                JOIN ValidRanked vr
                    ON vr.IdProduct = cp.IdProduct AND vr.IdCenter = cp.IdCenter
                    AND vr.rn <= cp.HistoryAduDays
                WHERE cp.deletedAt IS NULL AND cp.BufferType = 3
                GROUP BY cp.Id
            ),
            DateSpine AS (
                SELECT ws.CenterProductId, ws.WindowStartDate AS [Date]
                FROM WindowStart ws
                UNION ALL
                SELECT ds.CenterProductId, DATEADD(DAY, 1, ds.[Date])
                FROM DateSpine ds
                WHERE ds.[Date] < DATEADD(DAY, -1, @Today)
            ),
            RollingSums AS (
                SELECT ds.CenterProductId, ds.[Date], ra.RollingSum
                FROM DateSpine ds
                JOIN dbo.CenterProducts cp ON cp.Id = ds.CenterProductId
                CROSS APPLY (
                    SELECT SUM(h.Quantity) AS RollingSum
                    FROM dbo.Histories h
                    WHERE h.IdProduct = cp.IdProduct AND h.IdCenter = cp.IdCenter
                      AND h.deletedAt IS NULL AND h.DiscardStatus <> 2
                      AND h.Date <= ds.[Date]
                      AND h.Date >= DATEADD(DAY, -(CASE WHEN cp.LeadTime > cp.Frequency THEN cp.LeadTime ELSE cp.Frequency END), ds.[Date])
                ) ra
            ),
            MaxAccumulated AS (
                SELECT CenterProductId, MAX(ISNULL(RollingSum, 0)) AS Value
                FROM RollingSums
                GROUP BY CenterProductId
            )
            UPDATE cp
            SET
                cp.YellowZone = IIF(ISNULL(ma.Value, 0) = 0, 0, IIF(cp.LeadTime * cp.Adu < 0, 0, cp.LeadTime * cp.Adu)),
                cp.GreenZone = IIF(ISNULL(ma.Value, 0) = 0, 0, IIF(cp.Moq < 0, 0, cp.Moq)),
                cp.RedZoneBase = IIF(ISNULL(ma.Value, 0) = 0, 0, IIF(ISNULL(ma.Value, 0) - (cp.LeadTime * cp.Adu) < 0, 0, ISNULL(ma.Value, 0) - (cp.LeadTime * cp.Adu)))
            FROM dbo.CenterProducts cp
            LEFT JOIN MaxAccumulated ma ON ma.CenterProductId = cp.Id
            WHERE cp.deletedAt IS NULL AND cp.BufferType = 3
            OPTION (MAXRECURSION 0);
            """;
    }
}
