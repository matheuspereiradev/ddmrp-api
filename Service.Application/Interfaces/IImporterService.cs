using Service.Application.DTOs.Importer;
using Service.Application.DTOs.Procedure;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    // Read-only + run — importers are registered directly in the database (alongside the stored
    // procedure and, for imports, its table type) by whoever sets them up. No create/update/delete
    // is exposed through the API.
    public interface IImporterService
    {
        Task<ImporterGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedList<ImporterGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<List<ProcedureParameterDto>> GetParametersAsync(int id, CancellationToken cancellationToken = default);
        Task<ImporterRunResultDto> RunAsync(int id, Stream csvStream, CancellationToken cancellationToken = default);
    }
}
