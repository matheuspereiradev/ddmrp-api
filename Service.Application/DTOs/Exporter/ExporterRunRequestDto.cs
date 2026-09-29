namespace Service.Application.DTOs.Exporter
{
    public class ExporterRunRequestDto
    {
        // Keyed by parameter name (without '@'), matching GET /api/exporter/{id}/params. Values are
        // plain strings, converted server-side against the procedure's real parameter type — same
        // string-in/typed-out convention the CSV import path uses.
        public Dictionary<string, string?> Parameters { get; set; } = [];
    }
}
