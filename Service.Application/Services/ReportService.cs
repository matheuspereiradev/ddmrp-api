using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ICenterProductRepository _centerProductRepository;

        public ReportService(IReportRepository reportRepository, ICenterProductRepository centerProductRepository)
        {
            _reportRepository = reportRepository;
            _centerProductRepository = centerProductRepository;
        }

        public IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable(int[]? selectedCenters = null) =>
            _reportRepository.GetInventoryBufferManagementQueryable(selectedCenters);

        public Task<InventoryBufferManagementColorSummaryResult> SummarizeInventoryBufferManagementByColorAsync(IQueryable<InventoryBufferManagementRow> query, CancellationToken cancellationToken = default) =>
            _reportRepository.SummarizeInventoryBufferManagementByColorAsync(query, cancellationToken);

        public Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default) =>
            _reportRepository.GetOpenOrdersAsync(idCenter, idProduct, cancellationToken);

        public Task<List<InventoryHistoryRow>> GetInventoryHistoryAsync(int idCenter, int idProduct, DateTime dateStart, DateTime dateEnd, CancellationToken cancellationToken = default) =>
            _reportRepository.GetInventoryHistoryAsync(idCenter, idProduct, dateStart, dateEnd, cancellationToken);

        public async Task<List<ProjectedStockAlertRow>> GetProjectedStockAlertAsync(
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
            CancellationToken cancellationToken = default)
        {
            if (dateEnd < dateStart)
                throw new BadRequestException("dateEnd must not be earlier than dateStart.");

            var centerProduct = await _centerProductRepository.GetByProductAndCenterAsync(idProduct, idCenter, cancellationToken);
            if (centerProduct == null)
                throw new NotFoundException("CenterProduct not found.");

            return await _reportRepository.GetProjectedStockAlertAsync(
                idCenter, idProduct, dateStart, dateEnd,
                useAdu, useForecast, useInbounds, useOutbounds,
                accumulateInboundsToday, accumulateOutboundsToday,
                useFictionalOrders,
                cancellationToken);
        }

        public Task<List<BufferPenetrationRow>> GetBufferPenetrationAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            BufferPenetrationMode mode = BufferPenetrationMode.Netflow,
            CancellationToken cancellationToken = default)
        {
            if (dateEnd < dateStart)
                throw new BadRequestException("dateEnd must not be earlier than dateStart.");

            return _reportRepository.GetBufferPenetrationAsync(dateStart, dateEnd, idCenters, idProduct, mode, cancellationToken);
        }

        public Task<ItemsByBufferColorHistoryResult> GetItemsByBufferColorHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[]? idCenters,
            int? idProduct,
            CancellationToken cancellationToken = default)
        {
            if (dateEnd < dateStart)
                throw new BadRequestException("dateEnd must not be earlier than dateStart.");

            return _reportRepository.GetItemsByBufferColorHistoryAsync(dateStart, dateEnd, idCenters, idProduct, cancellationToken);
        }

        public Task<List<AccumulatedBufferHistoryRow>> GetAccumulatedBufferHistoryAsync(
            DateTime dateStart,
            DateTime dateEnd,
            int[] idCenters,
            CancellationToken cancellationToken = default)
        {
            if (dateEnd < dateStart)
                throw new BadRequestException("dateEnd must not be earlier than dateStart.");

            if (idCenters == null || idCenters.Length == 0)
                throw new BadRequestException("At least one idCenter must be informed.");

            return _reportRepository.GetAccumulatedBufferHistoryAsync(dateStart, dateEnd, idCenters, cancellationToken);
        }

        public async Task<QualifiedDemandReport> GetQualifiedDemandReportAsync(int idProduct, int idCenter, CancellationToken cancellationToken = default)
        {
            var centerProduct = await _centerProductRepository.GetByProductAndCenterAsync(idProduct, idCenter, cancellationToken);
            if (centerProduct == null)
                throw new NotFoundException("CenterProduct not found.");

            var report = await _reportRepository.GetQualifiedDemandReportAsync(idProduct, idCenter, cancellationToken);
            return report!;
        }
    }
}
