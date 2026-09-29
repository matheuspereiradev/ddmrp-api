using System.Globalization;

namespace Service.Infra.Data.Procedures
{
    // Converts raw CSV/request string values to the CLR value a given SQL Server type name expects,
    // for building a DataTable (import) or SqlParameter (export). Mirrors the same
    // string-in/typed-out shape as GenericTableIngestionWriter.ConvertValue, just keyed by SQL type
    // name instead of a CLR PropertyType, since these values are headed for ADO.NET, not EF.
    public static class SqlTypeConverter
    {
        // The CLR type a DataColumn must declare for a TVP column of this SQL type — SqlClient's
        // Structured parameter metadata (SmiMetaDataFromDataColumn) inspects DataColumn.DataType
        // directly and rejects a generic 'object' column with "Não há suporte para o tipo de
        // coluna... O tipo é 'Object'". Must return exactly the runtime type Convert(...) below
        // produces for the same sqlTypeName.
        public static Type GetClrType(string sqlTypeName) => sqlTypeName.ToLowerInvariant() switch
        {
            "int" => typeof(int),
            "bigint" => typeof(long),
            "smallint" => typeof(short),
            "tinyint" => typeof(byte),
            "decimal" or "numeric" or "money" or "smallmoney" => typeof(decimal),
            "float" => typeof(double),
            "real" => typeof(float),
            "bit" => typeof(bool),
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => typeof(string),
            "date" or "datetime" or "datetime2" or "smalldatetime" => typeof(DateTime),
            "uniqueidentifier" => typeof(Guid),
            _ => throw new InvalidOperationException($"Unsupported SQL type '{sqlTypeName}'.")
        };

        public static object Convert(string? raw, string sqlTypeName, bool isNullable)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (isNullable)
                    return DBNull.Value;

                throw new InvalidOperationException($"A value is required for non-nullable SQL type '{sqlTypeName}'.");
            }

            return sqlTypeName.ToLowerInvariant() switch
            {
                "int" => int.Parse(raw, CultureInfo.InvariantCulture),
                "bigint" => long.Parse(raw, CultureInfo.InvariantCulture),
                "smallint" => short.Parse(raw, CultureInfo.InvariantCulture),
                "tinyint" => byte.Parse(raw, CultureInfo.InvariantCulture),
                "decimal" or "numeric" or "money" or "smallmoney" => decimal.Parse(raw, NumberStyles.Any, CultureInfo.InvariantCulture),
                "float" => double.Parse(raw, CultureInfo.InvariantCulture),
                "real" => float.Parse(raw, CultureInfo.InvariantCulture),
                "bit" => ParseBit(raw),
                "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => raw,
                "date" or "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None),
                "uniqueidentifier" => Guid.Parse(raw),
                _ => throw new InvalidOperationException($"Unsupported SQL type '{sqlTypeName}'.")
            };
        }

        private static object ParseBit(string raw)
        {
            if (bool.TryParse(raw, out var boolValue))
                return boolValue;

            if (raw is "1" or "0")
                return raw == "1";

            throw new InvalidOperationException($"Invalid bit value '{raw}'.");
        }
    }
}
