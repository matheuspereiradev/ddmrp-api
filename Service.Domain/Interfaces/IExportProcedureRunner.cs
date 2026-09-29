using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IExportProcedureRunner
    {
        // Executes the procedure and streams its result set straight into outputStream as CSV
        // (header + rows), without materializing the whole result set in memory. Returns the row
        // count written. Throws InvalidOperationException on failure — same boundary convention
        // as IImportProcedureRunner.
        Task<int> RunAsync(Exporter exporter, IReadOnlyDictionary<string, string?> parameterValues, int userId, Stream outputStream, CancellationToken cancellationToken = default);
    }
}
