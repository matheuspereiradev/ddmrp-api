using Service.Application.DTOs.Exporter;
using Service.Application.DTOs.Procedure;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    // Read-only + run — same convention as IImporterService: exporters are registered directly in
    // the database, no create/update/delete through the API.
    public interface IExporterService
    {
        Task<ExporterGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedList<ExporterGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<List<ProcedureParameterDto>> GetParametersAsync(int id, CancellationToken cancellationToken = default);
        Task<ExporterRunResultDto> RunAsync(int id, Dictionary<string, string?> parameters, CancellationToken cancellationToken = default);
        Task<Stream> OpenDownloadStreamAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
