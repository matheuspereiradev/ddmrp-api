using Service.Domain.Report.Results;

namespace Service.Domain.Interfaces
{
    public interface IReportRepository
    {
        Task<List<InventoryBufferManagementRow>> GetInventoryBufferManagementAsync(CancellationToken cancellationToken = default);
    }
}
