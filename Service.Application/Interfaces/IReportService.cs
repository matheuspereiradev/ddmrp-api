using Service.Domain.Report.Results;

namespace Service.Application.Interfaces
{
    public interface IReportService
    {
        Task<List<InventoryBufferManagementRow>> GetInventoryBufferManagementAsync(CancellationToken cancellationToken = default);
    }
}
