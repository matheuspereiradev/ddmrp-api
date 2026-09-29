namespace Service.Application.DTOs.Procedure
{
    // Never includes @IdUser — it's always implicit (injected from the authenticated caller) and
    // excluded upstream by IProcedureCatalogService.
    public class ProcedureParameterDto
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsTableType { get; set; }
        public List<ProcedureParameterColumnDto>? Columns { get; set; }
    }
}
