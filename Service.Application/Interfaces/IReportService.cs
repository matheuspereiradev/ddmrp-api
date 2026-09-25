using Service.Domain.Enums;
using Service.Domain.Report.Results;

namespace Service.Application.Interfaces
{
    public interface IReportService
    {
        IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable(int[]? selectedCenters = null);
        Task<InventoryBufferManagementColorSummaryResult> SummarizeInventoryBufferManagementByColorAsync(IQueryable<InventoryBufferManagementRow> query, CancellationToken cancellationToken = default);
        Task<Dictionary<string, ColumnSummaryResult>> GetInventoryBufferManagementSummaryAsync(IQueryable<InventoryBufferManagementRow> query, IReadOnlyCollection<string> columns, CancellationToken cancellationToken = default);
        Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default);
        Task<List<InventoryHistoryRow>> GetInventoryHistoryAsync(int idCenter, int idProduct, DateTime dateStart, DateTime dateEnd, CancellationToken cancellationToken = default);
        Task<List<ProjectedStockAlertRow>> GetProjectedStockAlertAsync(
            int idCenter,
            int idProduct,
            DateTime dateStart,
            DateTime dateEnd,
            bool useAdu = true,
            bool useForecast = true,
            bool useInbounds = true,
            bool useOutbounds = true,
            bool accumulateInboundsToday = false,
            bool accumulateOutboundsToday = false,
            bool useFictionalOrders = true,
            CancellationToken cancellationToken = default);
        Task<List<BufferPenetrationRow>> GetBufferPenetrationAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            BufferPenetrationMode mode = BufferPenetrationMode.Netflow,
            CancellationToken cancellationToken = default);
        Task<ItemsByBufferColorHistoryResult> GetItemsByBufferColorHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            CancellationToken cancellationToken = default);
        Task<List<AccumulatedBufferHistoryRow>> GetAccumulatedBufferHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[] idCenters,
            CancellationToken cancellationToken = default);
    }
}
