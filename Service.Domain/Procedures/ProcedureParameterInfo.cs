namespace Service.Domain.Procedures
{
    // A single parameter of a stored procedure, as introspected from sys.parameters. @IdUser is
    // never represented here — callers exclude it up front, since it's always implicit (injected
    // from the authenticated caller) and never part of the CSV/request shape.
    public class ProcedureParameterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string SqlTypeName { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsTableType { get; set; }

        // Only set when IsTableType is true. Schema-qualified (e.g. "dbo.ZafImportType") — the
        // exact value SqlParameter.TypeName needs for a Structured parameter.
        public string? TableTypeFullName { get; set; }

        // Only set when IsTableType is true. Ordered by column_id — a DataTable built for this
        // parameter must add its columns in this exact order, since SqlClient matches a
        // Structured parameter's DataTable columns positionally, not by name.
        public List<ProcedureParameterColumnInfo>? Columns { get; set; }
    }
}
