namespace Service.Domain.Ingestion
{
    public class MappedRowResult
    {
        public Dictionary<string, string?> Values { get; set; } = [];
        public string? Error { get; set; }
    }
}
