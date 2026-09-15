using Service.Application.Interfaces;
using Service.Domain.Interfaces;
using Service.Domain.Report.Results;

namespace Service.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public IQueryable<InventoryBufferManagementRow> GetInventoryBufferManagementQueryable() =>
            _reportRepository.GetInventoryBufferManagementQueryable();

        public Task<List<OpenOrderRow>> GetOpenOrdersAsync(int? idCenter, int? idProduct, CancellationToken cancellationToken = default) =>
            _reportRepository.GetOpenOrdersAsync(idCenter, idProduct, cancellationToken);
    }
}
