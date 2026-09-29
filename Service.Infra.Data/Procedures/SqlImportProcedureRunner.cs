using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Procedures;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Procedures
{
    // Loads the whole CSV into a DataTable matching the procedure's table-valued parameter and
    // executes the procedure once (set-based) — never once per row. Every exception here is a
    // plain InvalidOperationException, per this project's boundary convention: Infra.Data throws
    // BCL exceptions, and Service.Application (ImporterService) translates them into a
    // BadRequestException.
    public class SqlImportProcedureRunner : IImportProcedureRunner
    {
        private readonly ApplicationDbContext _context;
        private readonly IProcedureCatalogService _catalog;

        public SqlImportProcedureRunner(ApplicationDbContext context, IProcedureCatalogService catalog)
        {
            _context = context;
            _catalog = catalog;
        }

        public async Task<ImportRunResult> RunAsync(Importer importer, Stream csvStream, int userId, CancellationToken cancellationToken = default)
        {
            var parameters = await _catalog.GetParametersAsync(importer.ProcedureName, cancellationToken);
            var tableParameters = parameters.Where(p => p.IsTableType).ToList();

            if (tableParameters.Count != 1)
                throw new InvalidOperationException($"Procedure '{importer.ProcedureName}' must declare exactly one table-valued parameter (besides @IdUser).");

            var tvp = tableParameters[0];
            var dataTable = ReadCsvIntoDataTable(csvStream, tvp);

            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            try
            {
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = importer.ProcedureName;

                command.Parameters.Add(new SqlParameter("@IdUser", SqlDbType.Int) { Value = userId });

                var tvpParameter = new SqlParameter($"@{tvp.Name}", SqlDbType.Structured)
                {
                    TypeName = tvp.TableTypeFullName,
                    Value = dataTable
                };
                command.Parameters.Add(tvpParameter);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Procedure '{importer.ProcedureName}' failed: {ex.Message}", ex);
            }

            return new ImportRunResult { RowCount = dataTable.Rows.Count };
        }

        private static DataTable ReadCsvIntoDataTable(Stream csvStream, ProcedureParameterInfo tvp)
        {
            var columns = tvp.Columns ?? [];

            var table = new DataTable();
            foreach (var column in columns)
            {
                var dataColumn = table.Columns.Add(column.Name, SqlTypeConverter.GetClrType(column.SqlTypeName));
                dataColumn.AllowDBNull = column.IsNullable;
            }

            using var streamReader = new StreamReader(csvStream);
            using var csv = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            });

            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];
            ValidateHeaders(headers, columns);

            var lineNumber = 1;
            while (csv.Read())
            {
                lineNumber++;
                var row = table.NewRow();

                foreach (var column in columns)
                {
                    var raw = csv.GetField(column.Name);
                    try
                    {
                        row[column.Name] = SqlTypeConverter.Convert(raw, column.SqlTypeName, column.IsNullable);
                    }
                    catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
                    {
                        throw new InvalidOperationException($"CSV line {lineNumber}: invalid value for column '{column.Name}' ({ex.Message}).", ex);
                    }
                }

                table.Rows.Add(row);
            }

            return table;
        }

        private static void ValidateHeaders(string[] headers, List<ProcedureParameterColumnInfo> columns)
        {
            var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);
            var expectedSet = new HashSet<string>(columns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

            var missing = expectedSet.Except(headerSet).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException($"CSV is missing required column(s): {string.Join(", ", missing)}.");

            var unexpected = headerSet.Except(expectedSet).ToList();
            if (unexpected.Count > 0)
                throw new InvalidOperationException($"CSV has unexpected column(s): {string.Join(", ", unexpected)}.");
        }
    }
}
