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

        public Task<List<InventoryBufferManagementRow>> GetInventoryBufferManagementAsync(CancellationToken cancellationToken = default) =>
            _reportRepository.GetInventoryBufferManagementAsync(cancellationToken);
    }
}
