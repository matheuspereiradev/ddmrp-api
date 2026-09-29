using Service.Domain.Entities;
using Service.Domain.Procedures;

namespace Service.Domain.Interfaces
{
    public interface IImportProcedureRunner
    {
        // Reads the whole csvStream into the procedure's single table-valued parameter and
        // executes it once (set-based), not once per CSV row. Throws InvalidOperationException
        // (never an AppException — this project's boundary convention keeps AppException out of
        // Infra.Data) for a header mismatch, a bad value, or a failed procedure execution; the
        // caller (Service.Application) is responsible for translating that into a BadRequestException.
        Task<ImportRunResult> RunAsync(Importer importer, Stream csvStream, int userId, CancellationToken cancellationToken = default);
    }
}
