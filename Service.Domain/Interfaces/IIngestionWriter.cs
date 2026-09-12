using Service.Domain.Ingestion;

namespace Service.Domain.Interfaces
{
    public interface IIngestionWriter
    {
        bool CanHandle(IngestionSourceConfig source);
        Task<IngestionWriteResult> WriteAsync(IngestionSourceConfig source, List<Dictionary<string, string?>> mappedRows, CancellationToken cancellationToken = default);
    }
}
