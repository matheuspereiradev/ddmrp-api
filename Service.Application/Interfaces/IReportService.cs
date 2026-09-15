using Service.Domain.Report.Results;

namespace Service.Application.Interfaces
{
    public interface IReportService
    {
        IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable();
        Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default);
    }
}
