using Service.Application.DTOs.Ingestion;

namespace Service.Application.Interfaces
{
    public interface IIngestionService
    {
        Task<List<IngestionRunResultDto>> RunAsync(string? view, CancellationToken cancellationToken = default);
    }
}
