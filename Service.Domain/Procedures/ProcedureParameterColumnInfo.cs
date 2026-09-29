namespace Service.Domain.Procedures
{
    public class ProcedureParameterColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string SqlTypeName { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
    }
}
