namespace Service.Domain.Entities
{
    public class Exporter : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string ProcedureName { get; set; }
    }
}
