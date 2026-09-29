namespace Service.Application.DTOs.Procedure
{
    public class ProcedureParameterColumnDto
    {
        public string Name { get; set; } = string.Empty;
        public string SqlType { get; set; } = string.Empty;
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
    }
}
