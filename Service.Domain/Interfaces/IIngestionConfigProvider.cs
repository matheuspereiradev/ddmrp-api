using Service.Domain.Ingestion;

namespace Service.Domain.Interfaces
{
    public interface IIngestionConfigProvider
    {
        Task<List<IngestionSourceConfig>> GetSourcesAsync(CancellationToken cancellationToken = default);
    }
}
