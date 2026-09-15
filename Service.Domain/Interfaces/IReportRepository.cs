using Service.Domain.Report.Results;

namespace Service.Domain.Interfaces
{
    public interface IReportRepository
    {
        Task<List<InventoryBufferManagementRow>> GetInventoryBufferManagementAsync(CancellationToken cancellationToken = default);
        Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default);
    }
}
