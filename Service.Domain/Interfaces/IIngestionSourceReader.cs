using Service.Domain.Ingestion;

namespace Service.Domain.Interfaces
{
    public interface IIngestionSourceReader
    {
        bool CanHandle(string type);
        IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(IngestionSourceConfig source, CancellationToken cancellationToken = default);
    }
}
