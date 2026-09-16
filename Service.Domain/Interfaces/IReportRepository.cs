using Service.Domain.Report.Results;

namespace Service.Domain.Interfaces
{
    public interface IReportRepository
    {
        IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable();
        Task<InventoryBufferManagementRow?> GetInventoryBufferManagementRowAsync(int idCenter, int idProduct, CancellationToken cancellationToken = default);
        Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default);
        Task<List<InventoryHistoryRow>> GetInventoryHistoryAsync(int idCenter, int idProduct, DateTime dateStart, DateTime dateEnd, CancellationToken cancellationToken = default);
    }
}
