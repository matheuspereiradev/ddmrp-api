using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Procedures
{
    // Executes the procedure and streams its SELECT result set straight into outputStream as CSV,
    // one row at a time via SqlDataReader — never materializes the whole result set in memory.
    // Same InvalidOperationException boundary convention as SqlImportProcedureRunner.
    public class SqlExportProcedureRunner : IExportProcedureRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly IProcedureCatalogService _catalog;

        public SqlExportProcedureRunner(ApplicationDbContext context, IProcedureCatalogService catalog)
        {
            _context = context;
            _catalog = catalog;
        }

        public async Task<int> RunAsync(Exporter exporter, IReadOnlyDictionary<string, string?> parameterValues, int userId, Stream outputStream, CancellationToken cancellationToken = default)
        {
            var parameters = await _catalog.GetParametersAsync(exporter.ProcedureName, cancellationToken);

            if (parameters.Any(p => p.IsTableType))
                throw new InvalidOperationException($"Procedure '{exporter.ProcedureName}' declares a table-valued parameter, which isn't supported for exporters.");

            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = exporter.ProcedureName;
            command.Parameters.Add(new SqlParameter("@IdUser", SqlDbType.Int) { Value = userId });

            foreach (var parameter in parameters)
            {
                if (!parameterValues.TryGetValue(parameter.Name, out var raw))
                    continue;

                object value;
                try
                {
                    value = SqlTypeConverter.Convert(raw, parameter.SqlTypeName, parameter.IsNullable);
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
                {
                    throw new InvalidOperationException($"Invalid value for parameter '{parameter.Name}': {ex.Message}", ex);
                }

                command.Parameters.Add(new SqlParameter($"@{parameter.Name}", value));
            }

            using var streamWriter = new StreamWriter(outputStream, leaveOpen: true);
            using var csv = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture));

            var rowCount = 0;
            try
            {
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                for (var i = 0; i < reader.FieldCount; i++)
                    csv.WriteField(reader.GetName(i));
                await csv.NextRecordAsync();

                while (await reader.ReadAsync(cancellationToken))
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                        csv.WriteField(reader.IsDBNull(i) ? null : reader.GetValue(i));

                    await csv.NextRecordAsync();
                    rowCount++;
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Procedure '{exporter.ProcedureName}' failed: {ex.Message}", ex);
            }

            await csv.FlushAsync();
            return rowCount;
        }
    }
}
