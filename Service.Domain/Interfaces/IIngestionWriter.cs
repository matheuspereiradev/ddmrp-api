using Service.Domain.Ingestion;

namespace Service.Domain.Interfaces
{
    public interface IIngestionWriter
    {
        bool CanHandle(string view);
        Task<IngestionWriteResult> WriteAsync(List<Dictionary<string, string?>> mappedRows, bool deleteNonSent, CancellationToken cancellationToken = default);
    }
}
