using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Service.Domain.Interfaces;
using Service.Domain.Procedures;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Procedures
{
    // Introspects sys.parameters/sys.types/sys.table_types/sys.columns directly — there is no EF
    // model for these system catalog views, so this is necessarily raw ADO.NET, not LINQ. Always
    // reads live from the database rather than caching, since the procedure can change independently
    // of the Importer/Exporter row that references it.
    public class SqlProcedureCatalogService : IProcedureCatalogService
    {
        private const string IdUserParameterName = "@IdUser";

        private readonly ApplicationDbContext _context;

        public SqlProcedureCatalogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProcedureParameterInfo>> GetParametersAsync(string procedureName, CancellationToken cancellationToken = default)
        {
            var connection = await GetOpenConnectionAsync(cancellationToken);
            try
            {
                var parameters = new List<ProcedureParameterInfo>();
                var tableTypeUserTypeIds = new Dictionary<ProcedureParameterInfo, int>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
DECLARE @ObjectId INT = OBJECT_ID(@ProcedureName, 'P');

SELECT
    pr.name AS ParameterName,
    ty.name AS SqlTypeName,
    pr.max_length AS MaxLength,
    pr.is_nullable AS IsNullable,
    tt.user_type_id AS TableTypeUserTypeId,
    SCHEMA_NAME(tt.schema_id) AS TableTypeSchema,
    tt.name AS TableTypeName
FROM sys.parameters pr
JOIN sys.types ty ON ty.user_type_id = pr.user_type_id
LEFT JOIN sys.table_types tt ON tt.user_type_id = pr.user_type_id
WHERE pr.object_id = @ObjectId AND pr.name <> @IdUserParameterName
ORDER BY pr.parameter_id;";
                    command.Parameters.Add(new SqlParameter("@ProcedureName", SqlDbType.NVarChar, 256) { Value = procedureName });
                    command.Parameters.Add(new SqlParameter("@IdUserParameterName", SqlDbType.NVarChar, 128) { Value = IdUserParameterName });

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var isTableType = !await reader.IsDBNullAsync(reader.GetOrdinal("TableTypeUserTypeId"), cancellationToken);

                        var info = new ProcedureParameterInfo
                        {
                            Name = ((string)reader["ParameterName"]).TrimStart('@'),
                            SqlTypeName = (string)reader["SqlTypeName"],
                            MaxLength = reader["MaxLength"] as short? is { } maxLength ? maxLength : null,
                            IsNullable = (bool)reader["IsNullable"],
                            IsTableType = isTableType
                        };

                        if (isTableType)
                        {
                            info.TableTypeFullName = $"{reader["TableTypeSchema"]}.{reader["TableTypeName"]}";
                            tableTypeUserTypeIds[info] = (int)reader["TableTypeUserTypeId"];
                        }

                        parameters.Add(info);
                    }
                }

                foreach (var (info, userTypeId) in tableTypeUserTypeIds)
                    info.Columns = await GetTableTypeColumnsAsync(connection, userTypeId, cancellationToken);

                return parameters;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Failed to read parameters for procedure '{procedureName}': {ex.Message}", ex);
            }
        }

        private static async Task<List<ProcedureParameterColumnInfo>> GetTableTypeColumnsAsync(SqlConnection connection, int tableTypeUserTypeId, CancellationToken cancellationToken)
        {
            var columns = new List<ProcedureParameterColumnInfo>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT c.name AS ColumnName, ty.name AS SqlTypeName, c.max_length AS MaxLength, c.is_nullable AS IsNullable
FROM sys.table_types tt
JOIN sys.columns c ON c.object_id = tt.type_table_object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE tt.user_type_id = @TableTypeUserTypeId
ORDER BY c.column_id;";
            command.Parameters.Add(new SqlParameter("@TableTypeUserTypeId", SqlDbType.Int) { Value = tableTypeUserTypeId });

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(new ProcedureParameterColumnInfo
                {
                    Name = (string)reader["ColumnName"],
                    SqlTypeName = (string)reader["SqlTypeName"],
                    MaxLength = reader["MaxLength"] as short? is { } maxLength ? maxLength : null,
                    IsNullable = (bool)reader["IsNullable"]
                });
            }

            return columns;
        }

        private async Task<SqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = (SqlConnection)_context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            return connection;
        }
    }
}
