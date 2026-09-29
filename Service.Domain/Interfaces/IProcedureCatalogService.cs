using Service.Domain.Procedures;

namespace Service.Domain.Interfaces
{
    public interface IProcedureCatalogService
    {
        // Always re-introspects the live procedure in the database rather than caching a
        // previously-stored signature — the procedure can be altered independently of the
        // Importer/Exporter row that references it.
        Task<List<ProcedureParameterInfo>> GetParametersAsync(string procedureName, CancellationToken cancellationToken = default);
    }
}
